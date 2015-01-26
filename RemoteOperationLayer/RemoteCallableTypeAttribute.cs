using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArdinRemoteOperations
{
    /// <summary>
    /// Mark class with attribute to indicate class has a method tagged with ClientServiceCallableFuncAttribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class RemoteCallableTypeAttribute : Attribute
    {
        public bool RegisterInDIContainer { get; set; }

        public RemoteCallableTypeAttribute() : this(false)
        {
        }

        public RemoteCallableTypeAttribute(bool registerInDIContainer)
        {
            this.RegisterInDIContainer = registerInDIContainer;
        }
    }
}
