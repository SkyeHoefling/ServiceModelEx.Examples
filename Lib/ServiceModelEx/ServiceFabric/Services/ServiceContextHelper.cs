using System.ServiceModel;
using System.ServiceModel.Channels;

using ServiceModelEx.Fabric;

namespace ServiceModelEx.ServiceFabric.Services
{
   internal static class ServiceContextHelper
   {
      public static ServiceContext Get(MessageHeaders headers)
      {
         return headers.GetHeader<GenericContext<ServiceContext>>(GenericContext<ServiceContext>.TypeName,GenericContext<ServiceContext>.TypeNamespace).Value;
      }
      public static void Add(MessageHeaders headers,ServiceContext serviceContext)
      {
         GenericContext<ServiceContext> context = new GenericContext<ServiceContext>(serviceContext);
         MessageHeader<GenericContext<ServiceContext>> genericHeader = new MessageHeader<GenericContext<ServiceContext>>(context);
         headers.Add(genericHeader.GetUntypedHeader(GenericContext<ServiceContext>.TypeName,GenericContext<ServiceContext>.TypeNamespace));
      }
      public static void Update(MessageHeaders headers,ServiceContext context)
      {
         int index = headers.FindHeader(GenericContext<ServiceContext>.TypeName,GenericContext<ServiceContext>.TypeNamespace);
         if(index > 0)
         {
            headers.RemoveAt(index);
            MessageHeader<GenericContext<ServiceContext>> genericHeader = new MessageHeader<GenericContext<ServiceContext>>(new GenericContext<ServiceContext>(context));
            headers.Add(genericHeader.GetUntypedHeader(GenericContext<ServiceContext>.TypeName,GenericContext<ServiceContext>.TypeNamespace));
         }
      }
   }
}
