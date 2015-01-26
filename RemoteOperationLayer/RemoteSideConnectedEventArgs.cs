using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArdinRemoteOperations
{
    public class RemoteSideConnectedEventArgs : EventArgs
    {
        public RemoteSideIDType RemoteID { get; private set; }

        public RemoteSideConnectedEventArgs(RemoteSideIDType remoteID)
        {
            if (remoteID == null)
            {
                throw new ArgumentNullException("remoteID");
            }

            this.RemoteID = remoteID;
        }
    }
}
