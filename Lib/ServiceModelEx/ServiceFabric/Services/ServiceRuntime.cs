using System;
using System.Diagnostics;
using System.Threading.Tasks;

using ServiceModelEx.Fabric;

namespace ServiceModelEx.ServiceFabric.Services.Runtime
{
   public class ServiceRuntime
   {
      public static Task RegisterServiceAsync(string serviceTypeName,Func<StatelessServiceContext,StatelessService> serviceFactory)
      {
         Debug.Assert(serviceFactory != null);

         FabricRuntime runtime = FabricRuntime.Create();
         StatelessService service = serviceFactory(new StatelessServiceContext());
         runtime.RegisterServiceType(serviceTypeName,service.GetType(),Test.TestHelper.IsUnderTest());
         return Task.CompletedTask;
      }
   }
}
