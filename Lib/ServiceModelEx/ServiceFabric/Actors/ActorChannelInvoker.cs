﻿// © 2016 IDesign Inc. All rights reserved 
//Questions? Comments? go to 
//http://www.idesign.net

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 618

namespace ServiceModelEx.ServiceFabric.Actors
{
   internal class ActorChannelInvoker<I> where I : class,IActor
   {
      public I Install(ChannelFactory<I> factory,ActorId actorid)
      {
         OperationInvoker invoker = new OperationInvoker(typeof(I),factory,actorid);
         return invoker.GetTransparentProxy() as I;
      }
      class OperationInvoker : RealProxy
      {
         readonly ChannelFactory<I> m_Factory;
         readonly ActorId m_ActorId;
         static readonly Assembly[] m_Assemblies;

         static OperationInvoker()
         {
            m_Assemblies = AppDomain.CurrentDomain.GetAssemblies();
         }
         public OperationInvoker(Type classToProxy,ChannelFactory<I> factory,ActorId actorId) : base(classToProxy)
         {
            m_Factory = factory;
            m_ActorId = actorId;
         }

         IMessage Retry(IMessage message,IMessage response)
         {
            Exception exception = null;
            Task result = (response as IMethodReturnMessage).ReturnValue as Task;
            if(result != null)
            {
               exception = result.Exception.InnerException;
            }
            else
            {
               exception = (response as IMethodReturnMessage).Exception;
            }

            //Only retry on transient timeout exceptions.
            if(!(exception is TimeoutException))
            {
               return response;
            }

            int retryCount = (int)message.Properties["retryCount"];
            retryCount++;
            if(retryCount < 5)
            {
               message.Properties["retryCount"] = retryCount;
               Thread.Sleep(5000 * retryCount);
               return Invoke(message);
            }
            return response;
         }
         Type FindExceptionType(string typeName)
         {
            Type type = null;
            try
            {
               IEnumerable<Assembly> assemblies = m_Assemblies.Where(assembly=>assembly.GetType(typeName) != null);
               type = assemblies.First().GetType(typeName);
               Debug.Assert(type != null,"Make sure this assembly (ServiceModelEx by default) contains the definition of the custom exception");
               Debug.Assert(type.IsSubclassOf(typeof(Exception)));
            }
            catch
            {
               type = typeof(Exception);
            }

            return type;
         }
         AggregateException ExtractException(ExceptionDetail detail)
         {
            AggregateException innerException = null;
            if(detail.InnerException != null)
            {
               innerException = ExtractException(detail.InnerException);
            }

            Type type = FindExceptionType(detail.Type);
            Type[] parameterTypes = null;
            if(innerException == null)
            {
               parameterTypes = new Type[] { typeof(string) };
            }
            else
            {
               parameterTypes = new Type[] { typeof(string),typeof(Exception) };
            }
            ConstructorInfo info = type.GetConstructor(parameterTypes);
            Debug.Assert(info != null,"Exception type " + detail.Type + " does not have suitable constructor");

            object[] parameters = null;
            if(innerException == null)
            {
               parameters = new object[] { detail.Message };
            }
            else
            {
               parameters = new object[] { detail.Message,innerException };
            }
            AggregateException exception = new AggregateException(Activator.CreateInstance(type,parameters) as Exception);
            Debug.Assert(exception != null);
            return exception;
         }
         Exception EvaluateException(Exception rootException)
         {
            Exception exception = rootException;
            Exception innerException = rootException.InnerException;

            //Since we're invoking, the outer exception will always be an invocation exception.
            if(innerException == null)
            {
               exception = rootException;
            }
            else if(!(innerException is FaultException))
            {
               exception = innerException;
            }
            else
            {
               if(!(innerException is FaultException<ExceptionDetail>))
               {
                  exception = innerException;
               }
               else
               {
                  exception = ExtractException((innerException as FaultException<ExceptionDetail>).Detail);
               }
            }
            return exception;
         }

         async Task<IMessage> InvokeAsync(IClientChannel channel,MethodCallMessageWrapper methodCallWrapper)
         {
            ReturnMessage response = await (methodCallWrapper.MethodBase.Invoke(channel,methodCallWrapper.Args) as Task).ContinueWith(result => new ReturnMessage(result,null,0,methodCallWrapper.LogicalCallContext,methodCallWrapper));
            return response;
         }
         public override IMessage Invoke(IMessage message)
         {
            MethodCallMessageWrapper methodCallWrapper = new MethodCallMessageWrapper((IMethodCallMessage)message);

            if((message as IMethodMessage).MethodName.Equals("get_Id"))
            {
               return new ReturnMessage(m_ActorId,null,0,methodCallWrapper.LogicalCallContext,methodCallWrapper);
            }
            if(!message.Properties.Contains("retryCount"))
            {
               message.Properties.Add("retryCount",0);
            }

            IMessage response = null;
            IClientChannel channel = null;
            try
            {
               channel = m_Factory.CreateChannel() as IClientChannel;
               channel.OperationTimeout = TimeSpan.MaxValue;
               //Do not add context. Treat each message as an activation request so that we can reassign the InstanceId on the service-side. 
               channel.Open();

               response = InvokeAsync(channel,methodCallWrapper).Result;

               Task result = ((response as IMethodReturnMessage).ReturnValue as Task);
               if(result.IsFaulted)
               {
                  response = new ReturnMessage(EvaluateException(result.Exception),methodCallWrapper);
                  response = Retry(message,response);
               }
               return response;
            }
            catch(TargetInvocationException exception)
            {
               if(response == null)
               {
                  response = new ReturnMessage(exception.InnerException,methodCallWrapper);
               }
               return Retry(message,response);
               throw exception.InnerException;
            }
            catch(TimeoutException exception)
            {
               if(response == null)
               {
                  response = new ReturnMessage(exception,methodCallWrapper);
               }
               return Retry(message,response);
               throw;
            }
            finally
            {
               if(channel.State != CommunicationState.Closed && channel.State != CommunicationState.Faulted)
               {
                  try
                  {
                     channel.Close();
                  }
                  catch
                  {
                     channel.Abort();
                  }
               }
               channel = null;
            }
         }
      }
   }
}

#pragma warning restore 618
