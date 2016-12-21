// © 2016 IDesign Inc. All rights reserved 
//Questions? Comments? go to 
//http://www.idesign.net

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Threading;
using System.Threading.Tasks;

using Moq;
using ServiceModelEx;
using ServiceModelEx.Fabric;
using ServiceModelEx.ServiceFabric.Actors;
using ServiceModelEx.ServiceFabric.Actors.Runtime;
using ServiceModelEx.ServiceFabric.Services;
using ServiceModelEx.ServiceFabric.Services.Remoting;
using ServiceModelEx.ServiceFabric.Services.Runtime;

namespace ServiceModelEx.ServiceFabric.Test
{
   public abstract class ServiceTestBase
   {
      static MethodInfo m_CreateInstanceDefinition = null;
      static MethodInfo m_RegisterActorAsyncDefinition = null;
      static MethodInfo m_RegisterServiceAsynMethod = null;
      static ServiceTestBase()
      {
         m_CreateInstanceDefinition = typeof(InProcFactory).GetMethod("CreateInstance",
                                                                      BindingFlags.NonPublic|BindingFlags.Static,
                                                                      null,
                                                                      new Type[] {typeof(IServiceBehavior),typeof(NetNamedPipeContextBinding),typeof(NetNamedPipeContextBinding)},
                                                                      null).GetGenericMethodDefinition();

         m_RegisterActorAsyncDefinition = typeof(ActorRuntime).GetMethod("RegisterActorAsync",
                                                                         BindingFlags.Public|BindingFlags.Static,
                                                                         null,
                                                                         new Type[] {typeof(TimeSpan),typeof(CancellationToken)},
                                                                         null).GetGenericMethodDefinition();

         m_RegisterServiceAsynMethod = typeof(ServiceRuntime).GetMethod("RegisterServiceAsync",
                                                                        BindingFlags.Public|BindingFlags.Static,
                                                                        null,
                                                                        new Type[] {typeof(string),typeof(Func<StatelessServiceContext,StatelessService>)},
                                                                        null);
      }

      Type[] m_ServicesUnderTest;
      Type[] m_ActorsUnderTest;

      public static Dictionary<Type,object> ServiceMocks
      {get; private set;}
      public static Dictionary<Type,object> ActorMocks
      {get; private set;}

      Type GetServiceType<I>() where I : class
      {
         Type serviceType = m_ServicesUnderTest.SingleOrDefault(service=>service.GetInterfaces().Where(contract=>contract == typeof(I)).Any());
         return serviceType;
      }
      Type GetActorType<I>() where I : class,IActor
      {
         Type actorType = m_ActorsUnderTest.SingleOrDefault(actor=>actor.GetInterfaces().Where(contract=>contract == typeof(I)).Any());
         return actorType;
      }

      [MethodImpl(MethodImplOptions.Synchronized)]
      protected void RegisterMocks(params object[] mocks)
      {
         foreach(object mock in mocks)
         {
            bool isActor = false;
            Func<Type,bool> hasContract = null;
            if(mock is Mock)
            {
               isActor = mock.GetType().GetGenericArguments()[0].IsSubclassOf(typeof(IActor));
               hasContract = (contract)=>contract == mock.GetType().GetGenericArguments()[0];
            }
            else
            {
               isActor = mock.GetType().GetInterfaces().Any(mockContract=>mockContract.IsSubclassOf(typeof(IActor)));
               hasContract = (contract)=>mock.GetType().GetInterfaces().Any(mockContract=>mockContract.Equals(contract));
            }

            if(isActor)
            {
               Debug.Assert(ActorMocks.Count > 0,"Invalid actor mock. Must first call harness.Setup() for actor types you wish to mock during test setup.");
               Type actorType = ActorMocks.Keys.FirstOrDefault(actor=>actor.GetInterfaces().Any(hasContract));
               Debug.Assert(actorType != null,"Invalid actor mock. Could not find registered actor mock for " + mock.GetType().ToString() + ". Must first call harness.Setup() for actor type during test setup.");
               if(actorType != null)
               {
                  ActorMocks[actorType] = mock;
               }
            }
            else
            {
               Debug.Assert(ServiceMocks.Count > 0,"Invalid service mock. Must first call harness.Setup() for service type during test setup.");
               Type serviceType = ServiceMocks.Keys.FirstOrDefault(actor=>actor.GetInterfaces().Any(hasContract));
               Debug.Assert(serviceType != null,"Invalid service mock. Could not find registered service mock for " + mock.GetType().ToString() + ". Must first call harness.Setup() for service type during test setup.");
               if(serviceType != null)
               {
                  ServiceMocks[serviceType] = mock;
               }
            }
         }
      }
      [MethodImpl(MethodImplOptions.Synchronized)]
      protected void UnregisterMocks()
      {
         if(ActorMocks.Count > 0)
         {
            Type[] actors = ActorMocks.Keys.ToArray();
            ActorMocks = actors.ToDictionary(k=>k,k=>(object)null);
         }
         if(ServiceMocks.Count > 0)
         {
            Type[] services = ServiceMocks.Keys.ToArray();
            ServiceMocks = services.ToDictionary(k=>k,k=>(object)null);
         }
      }
      [MethodImpl(MethodImplOptions.Synchronized)]
      public void Setup(params Type[] types)
      {
         Debug.Assert(types.Length > 0);
         foreach(Type type in types)
         {
            if(type.IsSubclassOf(typeof(ActorBase)))
            {
               MethodInfo registerActor = m_RegisterActorAsyncDefinition.MakeGenericMethod(type);
               Task result = registerActor.Invoke(null,new object[] {default(TimeSpan),default(CancellationToken)}) as Task;
               result.Wait();
            }
            else if (type.IsSubclassOf(typeof(StatelessService)))
            {
               Func<StatelessServiceContext,StatelessService> creationFunc = (context)=>Activator.CreateInstance(type,context) as StatelessService;
               Task result = m_RegisterServiceAsynMethod.Invoke(null,new object[] {type.Name+"Type",creationFunc}) as Task;
               result.Wait();
            }
            else
            {
               throw new InvalidOperationException("Invalid service type provided during test setup.");
            }
         }
         m_ServicesUnderTest = FabricRuntime.Services.Values.SelectMany(services=>services).Distinct().ToArray();
         ServiceMocks = m_ServicesUnderTest.ToDictionary(key=>key,key=>(object)null);
         m_ActorsUnderTest = FabricRuntime.Actors.Values.SelectMany(actors=>actors).Distinct().ToArray();
         ActorMocks = m_ActorsUnderTest.ToDictionary(key=>key,key=>(object)null);
      }
      [MethodImpl(MethodImplOptions.Synchronized)]
      public void Cleanup()
      {
         FabricRuntime.Actors.Clear();
         FabricRuntime.Services.Clear();
      }

      Uri ServiceAddress(Type serviceType)
      {
         //TODO: Support multiple app manifests.
         ApplicationManifestAttribute appManifest = serviceType.GetCustomAttributes<ApplicationManifestAttribute>().FirstOrDefault();
         return new Uri("fabric:/" + appManifest.ApplicationName + "/" + appManifest.ServiceName);
      }
      void MockServiceContext<I>(StatelessService target)
      {
         ServiceContext context = new ServiceContext();
         if(target == null)
         {
            Type serviceType = FabricRuntime.Services.Values.SelectMany(types=>types.Where(type=>type.GetInterfaces().Any(i=>i == typeof(I)))).FirstOrDefault();
            context.ServiceName = ServiceAddress(serviceType);
            ServiceContextHelper.Add(OperationContext.Current.OutgoingMessageHeaders,context);
         }
         else
         {
            context.ServiceName = ServiceAddress(target.GetType());

            FieldInfo request = typeof(OperationContext).GetField("request",BindingFlags.NonPublic|BindingFlags.Instance);
            Message message = Message.CreateMessage(MessageVersion.Soap12,"void");
            request.SetValue(OperationContext.Current,message);
            ServiceContextHelper.Add(OperationContext.Current.IncomingMessageHeaders,context);
         }
      }
      void MockActorId<I>(ActorBase target)
      {
         ActorId actorId = new ActorId("Test");
         if(target == null)
         {
            Type actorType = FabricRuntime.Actors.Values.SelectMany(types=>types.Where(type=>type.GetInterfaces().Any(i=>i == typeof(I)))).FirstOrDefault();
            Uri actorAddress = ServiceAddress(actorType);
            actorId.ApplicationName = actorAddress.Segments[1].TrimEnd('/');
            actorId.ActorInterfaceName = typeof(I).FullName;
            ActorIdHelper.Add(OperationContext.Current.OutgoingMessageHeaders,actorId);
         }
         else
         {
            Uri actorAddress = ServiceAddress(target.GetType());
            actorId.ApplicationName = actorAddress.Segments[1].TrimEnd('/');
            actorId.ActorInterfaceName = typeof(I).FullName;

            FieldInfo request = typeof(OperationContext).GetField("request",BindingFlags.NonPublic|BindingFlags.Instance);
            Message message = Message.CreateMessage(MessageVersion.Soap12,"void");
            request.SetValue(OperationContext.Current,message);
            ActorIdHelper.Add(OperationContext.Current.IncomingMessageHeaders,actorId);
         }
      }
      void MockEnvironment<I,S>(I callee,Action<I> callerMock,S state,params object[] mocks) where I : class
                                                                                             where S : class,new()
      {
         try
         {
            IClientChannel channel = callee as IClientChannel;
            if(channel == null)
            {
               if(callee is IActor)
               {
                  channel = ChannelFactory<I>.CreateChannel(BindingHelper.Actor.Binding(),new EndpointAddress("net.pipe://localhost")) as IClientChannel;
               }
               else
               {
                  channel = ChannelFactory<I>.CreateChannel(BindingHelper.Service.Default.Binding(),new EndpointAddress("net.pipe://localhost")) as IClientChannel;
               }
            }
            Debug.Assert(channel != null);

            using(channel)
            {
               using(OperationContextScope scope = new OperationContextScope(channel))
               {
                  if(callee is IActor)
                  {
                     MockActorId<I>(callee as ActorBase);
                     if(callee is ActorBase)
                     {
                        //Activate poco.
                        TestHelper.ActivateActor<S>(callee as IActor,state);
                     }
                  }
                  else
                  {
                     MockServiceContext<I>(callee as StatelessService);
                  }

                  RegisterMocks(mocks);
                  using (callee as IDisposable)
                  {
                     callerMock(callee);
                  }
                  UnregisterMocks();
               }
            }
         }
         finally
         {
            IClientChannel channel = callee as IClientChannel;
            if(channel != null)
            {
               if(channel.State != CommunicationState.Closed && channel.State != CommunicationState.Faulted)
               {
                  try
                  {
                     channel.Close();
                  }
                  catch
                  {
                     channel.Abort();
                  }
               }
            }
         }
      }
      void MockPocoEnvironment<I,S>(Type targetType,Action<I> callerMock,S state,params object[] mocks) where I : class
                                                                                                        where S : class,new() 
      {
         I poco = default(I);
         if(targetType.IsSubclassOf(typeof(ActorBase)))
         {
            poco = Activator.CreateInstance(targetType,new ActorService(),new ActorId("Test")) as I;
         }
         else if(targetType.IsSubclassOf(typeof(StatelessService)))
         {
            StatelessServiceContext context = new StatelessServiceContext
            {
               ServiceName = ServiceAddress(targetType)
            };
            poco = Activator.CreateInstance(targetType,context) as I;
         }
         else
         {
            throw new InvalidOperationException("Invalid service type provided during test.");
         }
         MockEnvironment<I,S>(poco,callerMock,state,mocks);
      }
      void MockServiceEnvironment<I,S>(Type targetType,Action<I> callerMock,S state,params object[] mocks) where I : class
                                                                                                           where S : class,new() 
      {
         I proxy = default(I);
         MethodInfo createInstance = m_CreateInstanceDefinition.MakeGenericMethod(targetType,typeof(I));
         if(targetType.IsSubclassOf(typeof(ActorBase)))
         {
            proxy = (I)createInstance.Invoke(null,new object[] 
            {
               new TestActorBehavior<S>(state),
               BindingHelper.Actor.ProxyBinding(),
               BindingHelper.Actor.Binding()
            });
         }
         else if(targetType.IsSubclassOf(typeof(StatelessService)))
         {
            proxy = (I)createInstance.Invoke(null,new object[] 
            {
               new TestServiceBehavior(),
               BindingHelper.Service.Default.ProxyBinding(),
               BindingHelper.Service.Default.Binding()
            });
         }
         else
         {
            throw new InvalidOperationException("Invalid service type provided during test.");
         }
         MockEnvironment<I,S>(proxy,callerMock,state,mocks);
      }

      public void TestActorPoco<I>(Action<I> callerMock,params object[] mocks) where I : class,IActor
      {
         TestActorPoco<I,object>(callerMock,null,mocks);
      }
      public void TestActorPoco<I,S>(Action<I> callerMock,S state,params object[] mocks) where I : class,IActor
                                                                                         where S : class,new()
      {
         MockPocoEnvironment<I,S>(GetActorType<I>(),callerMock,state,mocks);
      }
      public void TestActor<I>(Action<I> callerMock,params object[] mocks) where I : class,IActor
      {
         TestActor<I,object>(callerMock,null,mocks);
      }
      public void TestActor<I,S>(Action<I> callerMock,S state,params object[] mocks) where I : class,IActor 
                                                                                     where S : class,new()
      {
         MockServiceEnvironment<I,S>(GetActorType<I>(),callerMock,state,mocks);
      }

      public void TestServicePoco<I>(Action<I> callerMock,params object[] mocks) where I : class
      {
         MockPocoEnvironment<I,object>(GetServiceType<I>(),callerMock,null,mocks);
      }
      public void TestService<I>(Action<I> callerMock,params object[] mocks) where I : class
      {
         MockServiceEnvironment<I,object>(GetServiceType<I>(),callerMock,null,mocks);
      }
   }
}
