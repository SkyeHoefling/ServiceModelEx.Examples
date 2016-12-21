using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

using ServiceModelEx.Fabric;

namespace ServiceModelEx.ServiceFabric.Actors
{
   [DataContract]
   internal class InitializingContext
   {
      public static void Add(MessageHeaders headers)
      {
         GenericContext<InitializingContext> context = new GenericContext<InitializingContext>(new InitializingContext());
         MessageHeader<GenericContext<InitializingContext>> genericHeader = new MessageHeader<GenericContext<InitializingContext>>(context);
         headers.Add(genericHeader.GetUntypedHeader(GenericContext<InitializingContext>.TypeName,GenericContext<InitializingContext>.TypeNamespace));
      }
      public static bool Exists(MessageHeaders headers)
      {
         return headers.FindHeader(GenericContext<InitializingContext>.TypeName,GenericContext<InitializingContext>.TypeNamespace) >= 0;  
      }
   }

   public class StatefulActorInstanceProvider : IEndpointBehavior,IInstanceProvider
   {
      IInstanceProvider m_ServiceDurableInstanceProvider = null;

      object GetInstance(MessageHeaders headers,Type actorType)
      {
         ActorId id = ActorIdHelper.Get(headers);
         object instance = Activator.CreateInstance(actorType,new ActorService(),id);
         return instance;
      }
      public object GetInstance(InstanceContext instanceContext)
      {
         return m_ServiceDurableInstanceProvider.GetInstance(instanceContext);
      }
      public object GetInstance(InstanceContext instanceContext,Message message)
      {
         object serviceDurableInstance = m_ServiceDurableInstanceProvider.GetInstance(instanceContext,message);

         if(InitializingContext.Exists(message.Headers))
         {
            FieldInfo instance = serviceDurableInstance.GetType().GetField("instance",BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            instance.SetValue(serviceDurableInstance,GetInstance(message.Headers,instanceContext.Host.Description.ServiceType));
         }
         return serviceDurableInstance;
      }
      public void ReleaseInstance(InstanceContext instanceContext,object instance)
      {
         m_ServiceDurableInstanceProvider.ReleaseInstance(instanceContext,instance);
      }

      public void Validate(ServiceEndpoint endpoint)
      {}
      public void AddBindingParameters(ServiceEndpoint endpoint,BindingParameterCollection bindingParameters)
      {}
      public void ApplyClientBehavior(ServiceEndpoint endpoint,ClientRuntime clientRuntime)
      {}
      public void ApplyDispatchBehavior(ServiceEndpoint endpoint,EndpointDispatcher endpointDispatcher)
      {
         m_ServiceDurableInstanceProvider = endpointDispatcher.DispatchRuntime.InstanceProvider;
         endpointDispatcher.DispatchRuntime.InstanceProvider = this;
      }
   }
}