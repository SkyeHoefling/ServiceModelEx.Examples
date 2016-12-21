// © 2016 IDesign Inc. All rights reserved 
//Questions? Comments? go to 
//http://www.idesign.net

using System;
using System.Collections.Generic;
using System.ServiceModel;

#if ServiceModelEx_ServiceFabric
using ServiceModelEx.Fabric;
using ServiceModelEx.ServiceFabric.Services.Runtime;
using ServiceModelEx.ServiceFabric.Services.Communication.Runtime;
using ServiceModelEx.ServiceFabric.Services.Communication.Wcf.Runtime;
#else
using System.Fabric;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Communication.Wcf.Runtime;
#endif

namespace ServiceModelEx.ServiceFabric.Services.Communication.Wcf.Runtime
{
   public static class WcfHelper
   {
      static Type m_WcfListenerDefinition = null;
      static WcfHelper()
      {
         m_WcfListenerDefinition = typeof(WcfCommunicationListener<>).GetGenericTypeDefinition();
      }

      public static IEnumerable<ServiceInstanceListener> CreateListeners<T>(T serviceInstance) where T : StatelessService
      {
         List<ServiceInstanceListener> listeners = new List<ServiceInstanceListener>();
         foreach(Type contractType in typeof(T).GetInterfaces().Where(contract=>contract.GetCustomAttributes(typeof(ServiceContractAttribute),false).Length > 0))
         {
            Type wcfListener = m_WcfListenerDefinition.MakeGenericType(contractType);

            Func<StatelessServiceContext,ICommunicationListener> createListener = null;
            createListener = (context)=>Activator.CreateInstance(wcfListener,context,serviceInstance,null,contractType.Name) as ICommunicationListener;
            listeners.Add(new ServiceInstanceListener(createListener,contractType.Name));
         }
         return listeners;
      }
   }
}
