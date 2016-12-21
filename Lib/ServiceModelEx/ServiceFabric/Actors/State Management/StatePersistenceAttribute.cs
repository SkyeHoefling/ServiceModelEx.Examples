using System;

namespace ServiceModelEx.ServiceFabric.Actors.Runtime
{
   public enum StatePersistence
   {
      None = 0,
      Volatile = 1,
      Persisted = 2
   }

   [AttributeUsage(AttributeTargets.Class)]
   public sealed class StatePersistenceAttribute : Attribute
   {
      public StatePersistenceAttribute(StatePersistence statePersistence)
      {
         StatePersistence = statePersistence;
      }

      public StatePersistence StatePersistence
      {
         get;
      }
   }
}
