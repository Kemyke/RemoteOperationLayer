using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Channels;
using ArdinRemoteOperations;
using ArdinDIContainer;

namespace ArdinRemoteOperations.WCF
{
    public class WCFServiceHost : ServiceHost, IRemoteSide, IRemoteSideCommunicationContract
    {
        private ArdinDIContainer.IDIContainerLazyBoundObject<IWCFConfigManager> cm = null;
        private IDIContainer diContainer = null;
        private IRemoteSideCommunicationContract clientServiceContractInstance = null;

        public WCFServiceHost(IDIContainer diContainer, IRemoteSideCommunicationContract clientServiceContractInstance, Uri clientServiceAddress)
            : base(clientServiceContractInstance, clientServiceAddress)
        {
            this.ID = RemoteSideIDType.Parse(Guid.NewGuid().ToString());
            this.diContainer = diContainer;
            this.clientServiceContractInstance = clientServiceContractInstance;
            cm = diContainer.GetLazyBoundInstance<IWCFConfigManager>();

            ApplyConfiguration();

            this.AddServiceEndpoint(
                ServiceMetadataBehavior.MexContractName, 
                MetadataExchangeBindings.CreateMexTcpBinding(),
                String.Concat(new Uri(cm.Value.ClientServiceAddress).OriginalString, "/mex"));

            this.Closed += new EventHandler(clientServiceHost_StateChanged);
            this.Closing += new EventHandler(clientServiceHost_StateChanged);
            this.Faulted += new EventHandler(clientServiceHost_StateChanged);
            this.Opened += new EventHandler(clientServiceHost_StateChanged);
            this.Opening += new EventHandler(clientServiceHost_StateChanged);
        }

        private void clientServiceHost_StateChanged(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("WCFServiceHost.State changed: {0}", this.State.ToString());
            OnStateChanged();
        }

        public new void Open()
        {
            base.Open();
            System.Diagnostics.Debug.WriteLine("Listening on: {0} (WCF)", diContainer.GetLazyBoundInstance<IWCFConfigManager>().Value.ClientServiceAddress);
        }

        private Binding CreateBinding()
        {
            NetTcpBinding binding = new NetTcpBinding();

            WCFHelper.ApplyWCFBindingLimits(
                binding,
                cm.Value.ClientServiceMaxSizeInBytes,
                cm.Value.ClientServiceTimeoutInSecs);
            binding.Security.Mode = SecurityMode.None;
            binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.None;

            return binding;
        }

        protected override void ApplyConfiguration()
        {
            if (cm != null)
            {
                // visual studio debug hack START
                Uri[] arlBA = new Uri[this.BaseAddresses.Count];
                this.BaseAddresses.CopyTo(arlBA, 0);
                ServiceHost shDummy = new ServiceHost(this.Description.ServiceType, arlBA);
                foreach (IServiceBehavior o in shDummy.Description.Behaviors)
                {
                    if (o.GetType().ToString().Equals("Microsoft.VisualStudio.Diagnostics.ServiceModelSink.Behavior"))
                    {
                        this.Description.Behaviors.Add(o);
                        break;
                    }
                }
                // visual studio debug hack END


                ServiceDebugBehavior debug = new ServiceDebugBehavior();
                debug.IncludeExceptionDetailInFaults = true;
                this.Description.Behaviors.Add(debug);

                ServiceThrottlingBehavior throttling = new ServiceThrottlingBehavior();
                throttling.MaxConcurrentCalls = cm.Value.ClientServiceMaxConcurrentCalls;
                throttling.MaxConcurrentInstances = cm.Value.ClientServiceMaxConcurrentInstances;
                throttling.MaxConcurrentSessions = cm.Value.ClientServiceMaxConcurrentSessions;
                this.Description.Behaviors.Add(throttling);

                Binding binding = CreateBinding();
                ServiceEndpoint ep = this.AddServiceEndpoint(typeof(IRemoteSideCommunicationContract), binding, String.Empty);
                EndpointAddress epa = new EndpointAddress(ep.Address.Uri, EndpointIdentity.CreateDnsIdentity("localhost"), ep.Address.Headers);
                ep.Address = epa;

                WCFHelper.ApplyWCFEndpointLimits(ep, cm.Value.ClientServiceMaxItemsInObjectGraph);

                ServiceMetadataBehavior mb = new ServiceMetadataBehavior();
                this.Description.Behaviors.Add(mb);

                ServiceBehaviorAttribute sba = (ServiceBehaviorAttribute)(from a in this.Description.Behaviors
                                                                          where a.GetType() == typeof(ServiceBehaviorAttribute)
                                                                          select a).Single();

                sba.InstanceContextMode = InstanceContextMode.Single;
                sba.ConcurrencyMode = ConcurrencyMode.Multiple;
                sba.UseSynchronizationContext = false;
            }
        }

        public IRemoteSideCommunicationContract GetCurrentRemoteSideCommunicationContract()
        {
            IRemoteSideCommunicationContract ret = null;

            var cc = OperationContext.Current;
            if (cc != null)
            {
                ret = cc.GetCallbackChannel<IRemoteSideCommunicationContract>();
            }

            return ret;
        }



        public new RemoteCommunicationState State
        {
            get;
            private set;
        }

        public event EventHandler StateChanged;

        private void OnStateChanged()
        {
            if (this.StateChanged != null)
            {
                StateChanged(this, EventArgs.Empty);
            }
        }

        public RemoteResponse ExecuteRequest(RemoteRequest request)
        {
            RemoteResponse ret;
            ret = clientServiceContractInstance.ExecuteRequest(request);
            return ret;
        }

        #region IRemoteSide Members



        public RemoteSideIDType ID
        {
            get;
            private set;
        }

        #endregion
    }
}
