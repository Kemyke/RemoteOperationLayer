using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Timers;
using ArdinRemoteOperations;
using ArdinModel;
using ArdinDIContainer;

namespace ArdinRemoteOperations.WCF
{
    public class WCFServiceHostFactory : IRemoteSideFactory
    {
        private IDIContainer diContainer = null;
        public WCFServiceHostFactory(IDIContainer diContainer)
        {
            this.diContainer = diContainer;
        }

        #region IClientServiceHostFactory Members

        public IRemoteSide CreateInstance()
        {
            IRemoteSide ret = null;

            var cm = diContainer.GetLazyBoundInstance<IWCFConfigManager>().Value;
            var clientServiceInstance = diContainer.GetLazyBoundInstance<IRemoteSideCommunicationHandler>().Value;
            ret = new WCFServiceHost(diContainer, (IRemoteSideCommunicationContract)clientServiceInstance, new Uri(cm.ClientServiceAddress));
            clientServiceInstance.AssignRemoteSide(ret);

            return ret;
        }

        #endregion
    }
}
