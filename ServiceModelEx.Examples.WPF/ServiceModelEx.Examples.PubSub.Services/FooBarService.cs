using ServiceModelEx.Examples.PubSub.Contracts.ServiceContracts;

namespace ServiceModelEx.Examples.PubSub.Services
{
    public class FooBarService : DiscoveryPublishService<IFooBarServiceContract>, IFooBarServiceContract
    {
        public void Foo(string payload)
        {
            FireEvent(payload);
        }
    }
}
