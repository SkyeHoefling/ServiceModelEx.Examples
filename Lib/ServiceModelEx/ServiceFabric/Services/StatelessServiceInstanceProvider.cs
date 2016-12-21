using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

using ServiceModelEx.Fabric;

namespace ServiceModelEx.ServiceFabric.Services
{
   public class StatelessServiceInstanceProvider : IEndpointBehavior,IInstanceProvider
   {
      object GetInstance(Type serviceType)
      {
         ServiceContext context = GenericContext<ServiceContext>.Current.Value;
         object instance = Activator.CreateInstance(serviceType,new StatelessServiceContext(context.ServiceName));
         return instance;
      }
      public object GetInstance(InstanceContext instanceContext)
      {
         return GetInstance(instanceContext.Host.Description.ServiceType);
      }
      public object GetInstance(InstanceContext instanceContext,Message message)
      {
         return GetInstance(instanceContext.Host.Description.ServiceType);
      }
      public void ReleaseInstance(InstanceContext instanceContext,object instance)
      {}

      public void Validate(ServiceEndpoint endpoint)
      {}
      public void AddBindingParameters(ServiceEndpoint endpoint,BindingParameterCollection bindingParameters)
      {}
      public void ApplyClientBehavior(ServiceEndpoint endpoint,ClientRuntime clientRuntime)
      {}
      public void ApplyDispatchBehavior(ServiceEndpoint endpoint,EndpointDispatcher endpointDispatcher)
      {
         endpointDispatcher.DispatchRuntime.InstanceProvider = this;
      }
   }
}
