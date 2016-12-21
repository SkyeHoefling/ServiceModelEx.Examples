// © 2016 IDesign Inc. All rights reserved 
//Questions? Comments? go to 
//http://www.idesign.net

using System;
using System.ServiceModel;
using System.ServiceModel.Dispatcher;
using System.Threading.Tasks;

#pragma warning disable 618

namespace ServiceModelEx.ServiceFabric.Actors.Runtime
{
   [Serializable]
   public abstract class Actor : ActorBase,IStatefulActorManagement
   {
      public bool Completing
      {get; private set;}
      protected IActorStateManager StateManager
      {get;private set;}

      protected Actor(ActorService actorService,ActorId actorId)
      {
         StateManager = new ActorStateManager();
      }

      protected Task SaveStateAsync()
      {
         return Task.CompletedTask;
      }
      protected virtual Task OnCompleteAsync()
      {
         return Task.CompletedTask;
      }
      public async Task CompleteAsync()
      {
         ActorId actorId = GenericContext<ActorId>.Current.Value;

         await OnCompleteAsync().FlowWcfContext();

         Completing = true;
         OperationContext.Current.InstanceContext.Extensions.Add(new ActorInstanceContextProvider.ActorCompleted(actorId));
         DurableOperationContext.CompleteInstance();
         //Manage ActorIds here so file access failure will also abort the transaction.
         ActorManager.RemoveInstance(actorId);
      }
   }
}

#pragma warning restore 618
