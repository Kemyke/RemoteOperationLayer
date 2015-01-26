using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using System.Security;
using System.Runtime.Serialization.Formatters.Binary;
using ArdinHelpers;
using ArdinDIContainer;

namespace ArdinRemoteOperations
{
    public class RemoteOperationHandler : IRemoteOperationHandler
    {
        protected IDIContainer diContainer = null;

        public RemoteOperationHandler(IDIContainer diContainer) 
        {
            if (diContainer == null)
            {
                throw new ArgumentNullException("diContainer");
            }

            this.diContainer = diContainer;
        }

        public RemoteRequest CreateRequest(RemoteOperationDescriptor rso)
        {
            if (rso == null)
            {
                throw new ArgumentNullException("rso");
            }

            RemoteRequest ret = new RemoteRequest();
            ret.ExecuteOnRemoteSideOperation = rso;

            return ret;
        }

        public virtual object HandleResponse(RemoteResponse resp)
        {
            if (resp == null)
            {
                throw new ArgumentNullException("resp");
            }

            object ret = null;

            Exception ex = resp.ReturnValue as Exception;
            if (ex != null)
            {
                throw ex;
            }

            ret = resp.ReturnValue;

            return ret;
        }

        private static Assembly VersionMismatchCheckerAssemblyResolver(AssemblyName assemblyName)
        {
            Assembly ret = AppDomainHelper.LoadAssembly(assemblyName);
            if (assemblyName.Version != null && ret.GetName().Version != assemblyName.Version)
            {
                throw new VersionMismatchException(string.Format("Elvárt verzió: {0}! Kapott verzió: {1}!", assemblyName.Version, ret.GetName().Version)); //LOCSTR
            }

            return ret;
        }

        public virtual RemoteResponse ExecuteRequest(RemoteRequest request, Type callableTypeAttribute, Type callableFuncAttribute)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }
            if (callableTypeAttribute == null)
            {
                throw new ArgumentNullException("callableTypeAttribute");
            }
            if (callableFuncAttribute == null)
            {
                throw new ArgumentNullException("callableFuncAttribute");
            }

            RemoteResponse ret = new RemoteResponse();
            try
            {
                // get type
                Type interfaceType;
                interfaceType = TypeHelper.GetType(request.ExecuteOnRemoteSideOperation.InterfaceType, VersionMismatchCheckerAssemblyResolver, null);
                if (interfaceType != null)
                {
                    MethodInfo methodInfo = TypeHelper.GetMethodInfo(interfaceType, request.ExecuteOnRemoteSideOperation.MethodName, request.ExecuteOnRemoteSideOperation.Parameters);
                    if (methodInfo != null)
                    {
                        string calledMethodName = String.Concat(interfaceType.Name, "+", methodInfo.Name);
                        System.Diagnostics.Debug.WriteLine("{0}Call to: {1}", (request.RemoteID != null) ? String.Concat("[", request.RemoteID, "] ") : String.Empty, calledMethodName);

                        // sanity checks
                        if (!TypeHelper.HasCustomAttribute(interfaceType, callableTypeAttribute))
                        {
                            throw new InvalidOperationException(String.Format("Method {0} in type {1} called, but type not marked with {2}!", methodInfo.Name, interfaceType.Name, callableTypeAttribute)); //LOCSTR
                        }
                        var attrs = TypeHelper.GetCustomAttributes(methodInfo, callableFuncAttribute);
                        if (attrs.Length != 1)
                        {
                            throw new InvalidOperationException(String.Format("Method {0} in type {1} called, but method not marked with {2}!", methodInfo.Name, interfaceType.Name, callableFuncAttribute)); //LOCSTR
                        }

                        // get instance
                        var lbInstance = diContainer.GetLazyBoundInstance(interfaceType);
                        if (lbInstance.Value != null)
                        {
                            object instance = lbInstance.Value;
                            // call method
                            ret.ReturnValue = methodInfo.Invoke(instance, request.ExecuteOnRemoteSideOperation.Parameters);
                        }
                        else
                        {
                            throw new InvalidOperationException(string.Format("Interface {0} not available in DIContainer!", interfaceType.Name)); //LOCSTR
                        }

                    }
                    else
                    {
                        throw new InvalidOperationException(string.Format("Method {0} not found with parameters: {1}!", request.ExecuteOnRemoteSideOperation.MethodName, string.Join(",", request.ExecuteOnRemoteSideOperation.Parameters))); //LOCSTR
                    }
                }
                else
                {
                    throw new InvalidOperationException(string.Format("Interface {0} not found!", request.ExecuteOnRemoteSideOperation.InterfaceType)); //LOCSTR
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("While processing request: {1}{0}{1}{2}", request, Environment.NewLine, ex); //LOCSTR
                TargetInvocationException tex = ex as TargetInvocationException;
                if ((tex != null) && (tex.InnerException != null))
                {
                    ex = tex.InnerException;
                }
                ret.ReturnValue = ex;
            }

            return ret;
        }
    }
}
