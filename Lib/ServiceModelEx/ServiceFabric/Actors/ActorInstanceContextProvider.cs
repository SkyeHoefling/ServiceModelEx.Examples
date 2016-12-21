// © 2016 IDesign Inc. All rights reserved 
//Questions? Comments? go to 
//http://www.idesign.net

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Threading;

using ServiceModelEx.ServiceFabric.Actors.Runtime;

namespace ServiceModelEx.ServiceFabric.Actors
{
   internal class ActorInstanceContextProvider : IInstanceContextProvider,IEndpointBehavior
   {
      IInstanceContextProvider m_PreviousProvider = null;
      public ActorInstanceContextProvider()
      {}

      internal static Guid GetInstanceIdFromMessage(Message message)
      {
         string instanceId = null;
         ContextMessageProperty contextProperties = null;

         if(ContextMessageProperty.TryGet(message, out contextProperties))
         {
            if(contextProperties.Context.TryGetValue(ContextManager.InstanceIdKey, out instanceId))
            {
               return new Guid(instanceId);
            }
         }
         return Guid.Empty;
      }
      internal static bool RemoveInstanceIdFromMessage(Message message)
      {
         ContextMessageProperty contextProperties = null;

         bool found = ContextMessageProperty.TryGet(message, out contextProperties);
         if(found)
         {
            contextProperties.Context.Remove(ContextManager.InstanceIdKey);
         }
         return found;
      }
      internal static Guid GetNewInstanceIdFromMessage(Message message)
      {
         object instanceId = null;
         if(OperationContext.Current.IncomingMessageProperties.TryGetValue("newDurableInstanceIdProperty",out instanceId))
         {
            return (Guid)instanceId;
         }
         return Guid.Empty;
      }
      internal static void SetInstanceIdInMessage(Message message,string instanceId)
      {
         ContextMessageProperty contextProperties = null;
         if(!ContextMessageProperty.TryGet(message, out contextProperties))
         {
            contextProperties = new ContextMessageProperty(ContextManager.CreateContext(ContextManager.InstanceIdKey,instanceId));
            message.Properties.Add(ContextMessageProperty.Name,contextProperties);
         }
         else
         {
            if(!contextProperties.Context.ContainsKey(ContextManager.InstanceIdKey))
            {
               contextProperties.Context.Add(ContextManager.InstanceIdKey,instanceId);
            }
            else
            {
               contextProperties.Context[ContextManager.InstanceIdKey] = instanceId;
            }
         }
      }
      internal static bool HasPersistantStateProvider(Type serviceType)
      {
         bool hasPersistentprovider = false;
         StatePersistenceAttribute persistenceMode = serviceType.GetCustomAttribute<StatePersistenceAttribute>();
         if(persistenceMode != null)
         {
            hasPersistentprovider = persistenceMode.StatePersistence == StatePersistence.Persisted;
         }
         return hasPersistentprovider;
      }
      internal static ActorGarbageCollectionAttribute GetGarbageCollectionSettings(Type serviceType)
      {
         ActorGarbageCollectionAttribute collectionSettings = serviceType.GetCustomAttribute<ActorGarbageCollectionAttribute>();
         if(collectionSettings == null)
         {
            collectionSettings = new ActorGarbageCollectionAttribute();
         }
         return collectionSettings;
      }
      void SaveActorState(ActorId actorId,Guid durableInstanceId,Type serviceType)
      {
         bool statefulActor = HasPersistantStateProvider(serviceType);
         ActorInfo state = new ActorInfo
                           {
                              DurableInstanceId = durableInstanceId,
                              ActorId = actorId,
                              ActorImplementationType = serviceType,
                              ActorInterfaceType = serviceType.GetInterfaces().Single(contract=>!contract.Namespace.Equals(this.GetType().Namespace)),
                              IdleStartTime = DateTime.Now,
                              GarbageColllectionSettings = GetGarbageCollectionSettings(serviceType),
                              StatefulActor = statefulActor,
                           };
         ActorManager.SaveInstance(actorId,state,statefulActor);
      }

      internal class ActorCompleted : IExtension<InstanceContext>
      {
         public ActorId ActorId
         {get;set;}
         public ActorCompleted(ActorId actorId)
         {
            ActorId = actorId;
         }

         public void Attach(InstanceContext owner)
         {}
         public void Detach(InstanceContext owner)
         {}
      }
      readonly Mutex m_InstanceAccess = new Mutex(false);
      public InstanceContext GetExistingInstanceContext(Message message,IContextChannel channel)
      {
         //No OperationContext here!
         try
         {
            m_InstanceAccess.WaitOne();
            //Remove the instance id context if it exists. Treat each message as an activation request so that we can reassign the InstanceId on the service-side. 
            bool found = RemoveInstanceIdFromMessage(message);
            ActorId actorId = ActorIdHelper.Get(message.Headers);
            Guid instanceId = ActorManager.GetInstance(actorId);
            ActorManager.UpdateIdleTime(actorId);

            if((instanceId.Equals(Guid.Empty)) && (message.Headers.Action.Contains("Activate") == false))
            {
               //Message received after actor instance completed. Can only occur when a client improperly caches an ActorProxy after completing the instance.
               //Clear context and reset InstanceId so that InitializeInstanceContext can handle the message as an initiating request.
               message.Properties.Remove(ContextMessageProperty.Name);
            }
            else if(instanceId.Equals(Guid.Empty) == false)
            {
               //Always use the managed InstanceId. It may differ from the InstanceId in the ActorId context after an Actor completion.
               SetInstanceIdInMessage(message,instanceId.ToString());
            }
            InstanceContext context = m_PreviousProvider.GetExistingInstanceContext(message,channel);
            if(context != null)
            {
               m_InstanceAccess.ReleaseMutex();
            }
            return context;
         }
         catch(Exception exception)
         {
            m_InstanceAccess.ReleaseMutex();
            throw exception;
         }
      }
      public void InitializeInstanceContext(InstanceContext instanceContext,Message message,IContextChannel channel)
      {
         try
         {
            ActorId actorId = GenericContext<ActorId>.Current.Value;
            Guid instanceId = Guid.Empty;

            if(message.Headers.Action.Contains("Activate") == false)
            {
               OperationDescription operation = instanceContext.Host.Description.Endpoints.SelectMany(endpoint=>endpoint.Contract.Operations).FirstOrDefault(description=>description.Name.Equals(message.Headers.Action.Split('/').Last()));
               if(operation.IsInitiating == false)
               {
                  throw new InvalidOperationException("Actor instance previously completed. Cannot use operation '"  + operation.TaskMethod.Name + "' to activate new Actor state because it is marked as IsInitiating = false.");
               }
            }

            m_PreviousProvider.InitializeInstanceContext(instanceContext,message,channel);
            instanceId = GetNewInstanceIdFromMessage(message);
            if(instanceId.Equals(Guid.Empty) == false)
            {
               InitializingContext.Add(message.Headers);
               SaveActorState(actorId,instanceId,instanceContext.Host.Description.ServiceType);
            }
         }
         finally
         {
            m_InstanceAccess.ReleaseMutex();
         }
      }
      public bool IsIdle(InstanceContext instanceContext)
      {
         ActorCompleted actorCompleted = instanceContext.Extensions.Find<ActorCompleted>();
         if(actorCompleted == null)
         {
            return false;
         }
         else
         {
            bool idle = m_PreviousProvider.IsIdle(instanceContext);
            return idle;
         }
      }
      public void NotifyIdle(InstanceContextIdleCallback callback,InstanceContext instanceContext)
      {}

      public void AddBindingParameters(ServiceEndpoint endpoint,BindingParameterCollection bindingParameters)
      {}
      public void ApplyClientBehavior(ServiceEndpoint endpoint,ClientRuntime clientRuntime)
      {}
      public void ApplyDispatchBehavior(ServiceEndpoint endpoint,EndpointDispatcher endpointDispatcher)
      {
         Debug.Assert((m_PreviousProvider == null) || ((m_PreviousProvider != null) && (m_PreviousProvider.Equals(endpointDispatcher.DispatchRuntime.InstanceContextProvider))));

         m_PreviousProvider = endpointDispatcher.DispatchRuntime.InstanceContextProvider;
         endpointDispatcher.DispatchRuntime.InstanceContextProvider = this;
      }
      public void Validate(ServiceEndpoint endpoint)
      {}
   }
}