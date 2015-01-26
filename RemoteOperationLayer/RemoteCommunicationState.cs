using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArdinRemoteOperations
{
    /// <summary>
    /// A System.ServiceModel.CommunicationState duplikációja, hogy ne legyen WCF függés, ha nem akarjuk.
    /// </summary>
    public enum RemoteCommunicationState
    {
        NOTSET =0,
        Created,
        Opening,
        Opened,
        Closing,
        Closed,
        Faulted
    }
}
