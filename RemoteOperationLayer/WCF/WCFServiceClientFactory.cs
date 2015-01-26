using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using ArdinRemoteOperations;
using ArdinModel;
using ArdinDIContainer;

namespace ArdinRemoteOperations.WCF
{
    public class WCFServiceClientFactory : IRemoteSideFactory
    {
        private IDIContainer diContainer = null;
        public WCFServiceClientFactory(IDIContainer diContainer)
        {
            this.diContainer = diContainer;
        }

        public IRemoteSide CreateInstance()
        {
            IRemoteSide ret = null;

            // init binding
            var binding = new NetTcpBinding();
            binding.Security.Mode = SecurityMode.None;
            binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.None;

            var cm = diContainer.GetLazyBoundInstance<IWCFConfigManager>().Value;
            WCFHelper.ApplyWCFBindingLimits(
                binding,
                cm.ClientServiceMaxSizeInBytes,
                cm.ClientServiceTimeoutInSecs);

            // init endpoint address
            var endpointAddress = new EndpointAddress(cm.ClientServiceAddress);

            // create context
            var clientServiceInstance = diContainer.GetLazyBoundInstance<IRemoteSideCommunicationHandler>().Value;
            var instanceContext = new InstanceContext((IRemoteSideCommunicationContract)clientServiceInstance);

            // create client
            var cscc = new WCFServiceClient(instanceContext, binding, endpointAddress);
            clientServiceInstance.AssignRemoteSide(cscc);

            // init client
            WCFHelper.ApplyWCFEndpointLimits(cscc.Endpoint, cm.ClientServiceMaxItemsInObjectGraph);

            ret = cscc;

            return ret;
        }

    }
}
