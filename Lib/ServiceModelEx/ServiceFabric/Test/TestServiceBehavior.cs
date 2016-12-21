// © 2016 IDesign Inc. All rights reserved 
//Questions? Comments? go to 
//http://www.idesign.net

using System;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

using ServiceModelEx.Fabric;

namespace ServiceModelEx.ServiceFabric.Test
{
   internal class TestServiceBehavior : IServiceBehavior,IInstanceProvider
   {
      object GetInstance(Type serviceType)
      {
         ServiceContext context = GenericContext<ServiceContext>.Current.Value;
         object instance = Activator.CreateInstance(serviceType,new StatelessServiceContext(context.ServiceName));
         return instance;
      }
      public object GetInstance(System.ServiceModel.InstanceContext instanceContext,System.ServiceModel.Channels.Message message)
      {
         return GetInstance(instanceContext.Host.Description.ServiceType);
      }
      public object GetInstance(System.ServiceModel.InstanceContext instanceContext)
      {
         return GetInstance(instanceContext.Host.Description.ServiceType);
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
      {}
   }
}
