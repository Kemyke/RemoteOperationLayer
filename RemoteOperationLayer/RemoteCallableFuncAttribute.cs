using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArdinRemoteOperations
{
    /// <summary>
    /// Mark method with attribute if it can be a subject of ClientService call.
    /// Dont forget to mark containing this method with ClientServiceCallableTypeAttribute!
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class RemoteCallableFuncAttribute : Attribute
    {
    }
}
