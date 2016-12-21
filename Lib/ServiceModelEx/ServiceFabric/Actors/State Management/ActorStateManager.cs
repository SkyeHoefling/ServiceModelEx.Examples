using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

using ServiceModelEx.ServiceFabric.Data;

namespace ServiceModelEx.ServiceFabric.Actors.Runtime
{
   [Serializable]
   public class ActorStateManager : IActorStateManager
   {
      ConcurrentDictionary<string,object> m_State = new ConcurrentDictionary<string, object>();
      internal ActorStateManager()
      {}
      internal ActorStateManager(string stateName,object value)
      {
         m_State.TryAdd(stateName,value);
      }
      
      Task Execute(Action action)
      {
         try
         {
            action();
            return Task.CompletedTask;
         }
         catch (Exception exception)
         {
            return Task.FromException(exception);
         }
      }
      Task<T> Execute<T>(Func<T> action)
      {
         try
         {
            T result = action();
            return Task.FromResult(result);
         }
         catch (Exception exception)
         {
            return Task.FromException<T>(exception);
         }
      }

      public Task<T> AddOrUpdateStateAsync<T>(string stateName,T value,Func<string,T,T> updateValueFactory,CancellationToken cancellationToken = default(CancellationToken))
      {
         return Execute<T>(()=>(T)m_State.AddOrUpdate(stateName,value,(name,updateValue)=>updateValueFactory(name,(T)updateValue)));
      }
      public Task AddStateAsync<T>(string stateName,T value,CancellationToken cancellationToken = default(CancellationToken))
      {
         return Execute(()=>
                        {
                           if(m_State.TryAdd(stateName,value) == false)
                           {
                              throw new InvalidOperationException("An actor state with the given state name already exists.");
                           }
                        });
      }
      public Task<bool> ContainsStateAsync(string stateName,CancellationToken cancellationToken = default(CancellationToken))
      {
         return Execute<bool>(()=>m_State.ContainsKey(stateName));
      }
      public Task<T> GetOrAddStateAsync<T>(string stateName,T value,CancellationToken cancellationToken = default(CancellationToken))
      {
         return Execute<T>(()=>(T)m_State.GetOrAdd(stateName,value));
      }
      public Task<T> GetStateAsync<T>(string stateName,CancellationToken cancellationToken = default(CancellationToken))
      {
         return Execute<T>(()=>
                           {
                              object gotValue = null;
                              if(m_State.TryGetValue(stateName,out gotValue) == false)
                              {
                                 throw new InvalidOperationException("An actor state with the given state name does not exist.");
                              }
                              return (T)gotValue;
                           });
      }
      public Task<IEnumerable<string>> GetStateNamesAsync(CancellationToken cancellationToken = default(CancellationToken))
      {
         return Execute<IEnumerable<string>>(()=>m_State.Keys);
      }
      public Task RemoveStateAsync(string stateName,CancellationToken cancellationToken = default(CancellationToken))
      {
         return Execute(()=>
                        {
                           object removedValue = null;
                           if(m_State.TryRemove(stateName,out removedValue) == false)
                           {
                              throw new InvalidOperationException("An actor state with the given state name does not exist.");
                           }
                        });
      }
      public Task SetStateAsync<T>(string stateName,T value,CancellationToken cancellationToken = default(CancellationToken))
      {
         return AddOrUpdateStateAsync(stateName,value,(name,setValue)=>value);
      }
      public Task<bool> TryAddStateAsync<T>(string stateName,T value,CancellationToken cancellationToken = default(CancellationToken))
      {
         return Execute<bool>(()=>m_State.TryAdd(stateName,value));
      }
      public Task<ConditionalValue<T>> TryGetStateAsync<T>(string stateName,CancellationToken cancellationToken = default(CancellationToken))
      {
         return Execute<ConditionalValue<T>>(()=>
                                             {
                                                object gotValue = null;
                                                return new ConditionalValue<T>(m_State.TryGetValue(stateName,out gotValue),(T)gotValue);
                                             });
      }
      public Task<bool> TryRemoveStateAsync(string stateName,CancellationToken cancellationToken = default(CancellationToken))
      {
         return Execute(()=>
                        {
                           object removedValue = null;
                           return m_State.TryRemove(stateName,out removedValue);
                        });
      }
   }
}