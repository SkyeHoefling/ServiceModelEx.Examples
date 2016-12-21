// © 2016 IDesign Inc. All rights reserved 
//Questions? Comments? go to 
//http://www.idesign.net

using ServiceModelEx.Fabric;
using ServiceModelEx.ServiceFabric.Services.Communication.FabricTransport.Runtime;
using ServiceModelEx.ServiceFabric.Services.Communication.Runtime;
using ServiceModelEx.ServiceFabric.Services.Remoting.Runtime;

namespace ServiceModelEx.ServiceFabric.Services.Remoting.FabricTransport.Runtime
{
   public class FabricTransportServiceRemotingListener : IServiceRemotingListener, ICommunicationListener
   {
      public FabricTransportServiceRemotingListener(ServiceContext serviceContext,IService serviceImplementation,FabricTransportListenerSettings listenerSettings)
      {}
   }
}
