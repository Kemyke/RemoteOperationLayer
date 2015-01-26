using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArdinRemoteOperations.WCF;

namespace RemoteOperationLayerClientExample
{
    public class WCFConfigManager : IWCFConfigManager
    {
        public string ClientServiceAddress
        {
            get { return "net.tcp://localhost:10001/RemoteOperationsExample"; }
        }

        public int ClientServiceMaxItemsInObjectGraph
        {
            get { return int.MaxValue; }
        }

        public int ClientServiceMaxSizeInBytes
        {
            get { return int.MaxValue; }
        }

        public int ClientServiceMaxConcurrentCalls
        {
            get { return int.MaxValue; }
        }

        public int ClientServiceMaxConcurrentInstances
        {
            get { return int.MaxValue; }
        }

        public int ClientServiceMaxConcurrentSessions
        {
            get { return int.MaxValue; }
        }

        public int ClientServiceTimeoutInSecs
        {
            get { return 10; }
        }
    }
}
