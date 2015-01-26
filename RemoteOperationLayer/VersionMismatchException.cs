using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Permissions;
using System.Runtime.Serialization;

namespace ArdinRemoteOperations
{
    [Serializable]
    public class VersionMismatchException : Exception
    {
        public VersionMismatchException(string message) : base(message) { }

        public VersionMismatchException(string message, Exception innerException) : base(message, innerException) { }

        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        protected VersionMismatchException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
