// © 2016 IDesign Inc. All rights reserved 
//Questions? Comments? go to 
//http://www.idesign.net

using System;
using ServiceModelEx.Fabric;

namespace ServiceModelEx.ServiceFabric.Services.Communication.Runtime
{
   public sealed class ServiceInstanceListener
   {
      internal Func<StatelessServiceContext,ICommunicationListener> CreateCommunicationListener
      {get; set;}
      public ServiceInstanceListener(Func<StatelessServiceContext,ICommunicationListener> createCommunicationListener,string name = "")
      {
         CreateCommunicationListener = createCommunicationListener;
      }
   }
}
