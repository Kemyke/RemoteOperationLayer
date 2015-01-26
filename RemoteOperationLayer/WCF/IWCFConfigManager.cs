using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArdinRemoteOperations.WCF
{
    public interface IWCFConfigManager
    {
        string ClientServiceAddress { get; }
        int ClientServiceMaxItemsInObjectGraph { get; }
        int ClientServiceMaxSizeInBytes { get; }
        int ClientServiceMaxConcurrentCalls { get; }
        int ClientServiceMaxConcurrentInstances { get; }
        int ClientServiceMaxConcurrentSessions { get; }
        int ClientServiceTimeoutInSecs { get; }
    }
}
