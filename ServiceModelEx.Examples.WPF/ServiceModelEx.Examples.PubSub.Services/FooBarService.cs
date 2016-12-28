using ServiceModelEx.Examples.PubSub.Contracts.ServiceContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceModelEx;

namespace ServiceModelEx.Examples.PubSub.Services
{
    public class FooBarService : DiscoveryPublishService<IFooBarServiceContract>, IFooBarServiceContract
    {
        public void Foo()
        {
            FireEvent();
        }
    }
}
