using System;

namespace ServiceModelEx.Fabric
{
   public sealed class StatelessServiceContext : ServiceContext
   {
      internal StatelessServiceContext()
      {}
      internal StatelessServiceContext(Uri serviceName)
      {
         ServiceName = serviceName;
      }
   }
}
