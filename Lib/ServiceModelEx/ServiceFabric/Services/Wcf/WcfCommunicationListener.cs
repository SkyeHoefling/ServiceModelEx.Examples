// © 2016 IDesign Inc. All rights reserved 
//Questions? Comments? go to 
//http://www.idesign.net

using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

using ServiceModelEx.Fabric;
using ServiceModelEx.ServiceFabric.Services.Communication.Runtime;

namespace ServiceModelEx.ServiceFabric.Services.Communication.Wcf.Runtime
{
   public class WcfCommunicationListener
   {
      internal Type InterfaceType
      {get;set;}
      internal Type ImplementationType
      {get;set;}
      public NetTcpBinding Binding 
      {get; set;}
   }

   public class WcfCommunicationListener<I> : WcfCommunicationListener,ICommunicationListener
   {
      public WcfCommunicationListener(ServiceContext serviceContext,I serviceInstance,Binding listenerBinding = null,string endpointResourceName = null)
      {
         InterfaceType = typeof(I);
         ImplementationType = serviceInstance.GetType();
         Binding = BindingHelper.Service.Wcf.ServiceBinding();
      }
   }
}
