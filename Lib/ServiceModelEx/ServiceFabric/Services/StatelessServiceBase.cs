// © 2016 IDesign Inc. All rights reserved 
//Questions? Comments? go to 
//http://www.idesign.net

using System.Collections.Generic;

using ServiceModelEx.Fabric;
using ServiceModelEx.ServiceFabric.Services.Communication.Runtime;

namespace ServiceModelEx.ServiceFabric.Services.Runtime
{
   public abstract class StatelessService
   {
      protected StatelessService(StatelessServiceContext context)
      {
         Context = context;
         ServiceInstanceListeners = CreateServiceInstanceListeners();
      }
      protected abstract IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners();
      public StatelessServiceContext Context
      {get; private set;}
      public IEnumerable<ServiceInstanceListener> ServiceInstanceListeners 
      {get; private set;}
   }
}
