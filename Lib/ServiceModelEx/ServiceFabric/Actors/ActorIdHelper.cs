using System.ServiceModel;
using System.ServiceModel.Channels;

namespace ServiceModelEx.ServiceFabric.Actors
{
   internal static class ActorIdHelper
   {
      public static ActorId Get(MessageHeaders headers)
      {
         return headers.GetHeader<GenericContext<ActorId>>(GenericContext<ActorId>.TypeName,GenericContext<ActorId>.TypeNamespace).Value;
      }
      public static void Add(MessageHeaders headers,ActorId actorId)
      {
         GenericContext<ActorId> context = new GenericContext<ActorId>(actorId);
         MessageHeader<GenericContext<ActorId>> genericHeader = new MessageHeader<GenericContext<ActorId>>(context);
         headers.Add(genericHeader.GetUntypedHeader(GenericContext<ActorId>.TypeName,GenericContext<ActorId>.TypeNamespace));
      }
      public static void Update(MessageHeaders headers,ActorId actorId)
      {
         int index = headers.FindHeader(GenericContext<ActorId>.TypeName,GenericContext<ActorId>.TypeNamespace);
         if(index > 0)
         {
            headers.RemoveAt(index);
            MessageHeader<GenericContext<ActorId>> genericHeader = new MessageHeader<GenericContext<ActorId>>(new GenericContext<ActorId>(actorId));
            headers.Add(genericHeader.GetUntypedHeader(GenericContext<ActorId>.TypeName,GenericContext<ActorId>.TypeNamespace));
         }
      }
   }
}
