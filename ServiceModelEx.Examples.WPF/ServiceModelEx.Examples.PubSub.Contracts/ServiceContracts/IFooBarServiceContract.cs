using System.ServiceModel;

namespace ServiceModelEx.Examples.PubSub.Contracts.ServiceContracts
{
    [ServiceContract]
    public interface IFooBarServiceContract
    {
        [OperationContract(IsOneWay = true)]
        void Foo();
    }
}
