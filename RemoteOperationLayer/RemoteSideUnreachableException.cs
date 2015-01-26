using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArdinRemoteOperations
{
    [Serializable]
    public class RemoteSideUnreachableException : Exception
    {
        public RemoteSideUnreachableException(string message) : base(message) { }
        public RemoteSideUnreachableException(string message, Exception innerException) : base(message, innerException) { }
    }
}
