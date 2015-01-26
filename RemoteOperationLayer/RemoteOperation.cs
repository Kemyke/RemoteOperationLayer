using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArdinRemoteOperations
{
    [Serializable]
    public class RemoteOperationDescriptor
    {
        public RemoteOperationDescriptor(string interfaceType, string methodName, params object[] paramters)
        {
            if (interfaceType == null)
            {
                throw new ArgumentNullException("interfaceType");
            }

            if (methodName == null)
            {
                throw new ArgumentNullException("methodName");
            }

            InterfaceType = interfaceType;
            MethodName = methodName;
            Parameters = paramters;
        }

        public string InterfaceType
        {
            get;
            set;
        }
        
        public string MethodName
        {
            get;
            set;
        }
        public object[] Parameters
        {
            get;
            set;
        }
    }
}
