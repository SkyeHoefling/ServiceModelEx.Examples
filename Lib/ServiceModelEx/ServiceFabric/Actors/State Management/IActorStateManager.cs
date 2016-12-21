using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using ServiceModelEx.ServiceFabric.Data;

namespace ServiceModelEx.ServiceFabric.Actors.Runtime
{
   public interface IActorStateManager
   {
      Task<T> AddOrUpdateStateAsync<T>(string stateName,T addValue,Func<string,T,T> updateValueFactory,CancellationToken cancellationToken = default(CancellationToken));
      Task AddStateAsync<T>(string stateName,T value,CancellationToken cancellationToken = default(CancellationToken));
      Task<bool> ContainsStateAsync(string stateName,CancellationToken cancellationToken = default(CancellationToken));
      Task<T> GetOrAddStateAsync<T>(string stateName,T value,CancellationToken cancellationToken = default(CancellationToken));
      Task<T> GetStateAsync<T>(string stateName,CancellationToken cancellationToken = default(CancellationToken));
      Task<IEnumerable<string>> GetStateNamesAsync(CancellationToken cancellationToken = default(CancellationToken));
      Task RemoveStateAsync(string stateName,CancellationToken cancellationToken = default(CancellationToken));
      Task SetStateAsync<T>(string stateName,T value,CancellationToken cancellationToken = default(CancellationToken));
      Task<bool> TryAddStateAsync<T>(string stateName,T value,CancellationToken cancellationToken = default(CancellationToken));
      Task<ConditionalValue<T>> TryGetStateAsync<T>(string stateName,CancellationToken cancellationToken = default(CancellationToken));
      Task<bool> TryRemoveStateAsync(string stateName,CancellationToken cancellationToken = default(CancellationToken));
   }
}
