using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArdinRemoteOperations
{
    [Serializable]
    public class RemoteSideFaultedException : Exception
    {
        public RemoteSideFaultedException(string message) : base(message) { }
        public RemoteSideFaultedException(string message, Exception innerException) : base(message, innerException) { }
    }
}
