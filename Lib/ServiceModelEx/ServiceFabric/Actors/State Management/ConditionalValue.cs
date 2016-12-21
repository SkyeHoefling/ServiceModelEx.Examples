namespace ServiceModelEx.ServiceFabric.Data
{
   public struct ConditionalValue<T>
   {
      public ConditionalValue(bool hasValue,T value)
      {
         HasValue = hasValue;
         Value = value;
      }
      public bool HasValue
      {get;private set;}
      public T Value
      {get;private set;}
   }
}