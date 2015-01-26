using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArdinRemoteOperations
{
    public class RemoteSideDisconnectedEventArgs : EventArgs
    {
        public RemoteSideIDType RemoteID { get; private set; }

        public RemoteSideDisconnectedEventArgs(RemoteSideIDType remoteID)
        {
            if (remoteID == null)
            {
                throw new ArgumentNullException("remoteID");
            }

            this.RemoteID = remoteID;
        }
    }
}
