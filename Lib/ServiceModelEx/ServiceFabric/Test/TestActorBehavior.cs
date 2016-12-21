// © 2016 IDesign Inc. All rights reserved 
//Questions? Comments? go to 
//http://www.idesign.net

using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

using ServiceModelEx.ServiceFabric.Actors;

namespace ServiceModelEx.ServiceFabric.Test
{
   internal class TestActorBehavior<S> : IServiceBehavior,IInstanceProvider where S : class,new()
   {
      S State 
      {get;set;}

      public TestActorBehavior(S state)
      {
         State = state;
      }

      object GetInstance(MessageHeaders headers,Type serviceType)
      {
         ActorId id = ActorIdHelper.Get(headers);
         object instance = Activator.CreateInstance(serviceType,new ActorService(),id);
         TestHelper.ActivateActor<S>(instance as IActor,State);
         return instance;
      }
      public object GetInstance(System.ServiceModel.InstanceContext instanceContext,System.ServiceModel.Channels.Message message)
      {
         return GetInstance(message.Headers,instanceContext.Host.Description.ServiceType);
      }
      public object GetInstance(System.ServiceModel.InstanceContext instanceContext)
      {
         return GetInstance(null,instanceContext.Host.Description.ServiceType);
      }
      public void ReleaseInstance(System.ServiceModel.InstanceContext instanceContext,object instance)
      {}

      class InstanceProviderBehavior : IEndpointBehavior
      {
         IInstanceProvider InstanceProvider
         {get;set;}
         public InstanceProviderBehavior(IInstanceProvider instanceProivder)
         {
            InstanceProvider = instanceProivder;
         }

         public void AddBindingParameters(ServiceEndpoint endpoint,System.ServiceModel.Channels.BindingParameterCollection bindingParameters)
         {}
         public void ApplyClientBehavior(ServiceEndpoint endpoint,ClientRuntime clientRuntime)
         {}
         public void ApplyDispatchBehavior(ServiceEndpoint endpoint,EndpointDispatcher endpointDispatcher)
         {
            endpointDispatcher.DispatchRuntime.InstanceProvider = InstanceProvider;
         }
         public void Validate(ServiceEndpoint endpoint)
         {}
      }

      public void AddBindingParameters(ServiceDescription serviceDescription,System.ServiceModel.ServiceHostBase serviceHostBase,System.Collections.ObjectModel.Collection<ServiceEndpoint> endpoints,System.ServiceModel.Channels.BindingParameterCollection bindingParameters)
      {}
      public void ApplyDispatchBehavior(ServiceDescription serviceDescription,System.ServiceModel.ServiceHostBase serviceHostBase)
      {
         foreach (ServiceEndpoint endpoint in serviceDescription.Endpoints)
         {
            endpoint.EndpointBehaviors.Add(new InstanceProviderBehavior(this));
         }
      }
      public void Validate(ServiceDescription serviceDescription,System.ServiceModel.ServiceHostBase serviceHostBase)
      {
         if(serviceDescription.Behaviors.Find<ActorStateProviderAttribute>() != null)
         {
            serviceDescription.Behaviors.Remove<ActorStateProviderAttribute>();
         }
      }
   }
}
