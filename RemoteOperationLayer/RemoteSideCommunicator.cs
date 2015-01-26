using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Timers;

namespace ArdinRemoteOperations
{
    public class RemoteSideCommunicator : IRemoteSideCommunicationHandler, IRemoteSideCommunicationContract
    {
        protected IRemoteOperationHandler remoteOperationHandler = null;
        
        protected Dictionary<RemoteSideIDType, Tuple<DateTime, IRemoteSideCommunicationContract>> disconnectedRemoteSideList = new Dictionary<RemoteSideIDType, Tuple<DateTime, IRemoteSideCommunicationContract>>();
        protected IRemoteSide remoteSide = null;

        public RemoteSideCommunicator(IRemoteOperationHandler remoteOperationHandler)
        {
            if (remoteOperationHandler == null)
            {
                throw new ArgumentNullException("remoteOperationHandler");
            }

            this.remoteOperationHandler = remoteOperationHandler;
        }

        public RemoteSideCommunicator(IRemoteOperationHandler remoteOperationHandler, Tuple<RemoteSideIDType, IRemoteSideCommunicationContract> alreadyKnownRemoteSide) :this(remoteOperationHandler)
        {
            if (alreadyKnownRemoteSide == null)
            {
                throw new ArgumentNullException("alreadyKnownRemoteSide");
            }

            remoteSideIDsByCommunicationContractDict.Add(alreadyKnownRemoteSide.Item2, alreadyKnownRemoteSide.Item1);
            communicationContractsByRemoteSideIDDict.Add(alreadyKnownRemoteSide.Item1, alreadyKnownRemoteSide.Item2);
        }

        protected virtual void DisconnectRemoteSide(RemoteSideIDType remoteID, IRemoteSideCommunicationContract serviceContract)
        {
            remoteSideIDsByCommunicationContractDict.Remove(serviceContract);
            communicationContractsByRemoteSideIDDict.Remove(remoteID);

            OnRemoteSideDisconnected(remoteID);
        }

        protected SpinLock syncRoot = new SpinLock();
        protected Dictionary<IRemoteSideCommunicationContract, RemoteSideIDType> remoteSideIDsByCommunicationContractDict = new Dictionary<IRemoteSideCommunicationContract, RemoteSideIDType>();
        protected Dictionary<RemoteSideIDType, IRemoteSideCommunicationContract> communicationContractsByRemoteSideIDDict = new Dictionary<RemoteSideIDType, IRemoteSideCommunicationContract>();


        public RemoteSideIDType CurrentRemoteSideID
        {
            get
            {
                ThrowIfNotInitialized();

                RemoteSideIDType ret = null;

                var cb = remoteSide.GetCurrentRemoteSideCommunicationContract();
                if (cb != null)
                {
                    bool lockTaken = false;
                    try
                    {
                        syncRoot.Enter(ref lockTaken);
                        remoteSideIDsByCommunicationContractDict.TryGetValue(cb, out ret);
                    }
                    finally
                    {
                        if (lockTaken)
                        {
                            syncRoot.Exit();
                        }
                    }

                }
                return ret;
            }
        }

        protected virtual void NewRemoteSideConnected(RemoteSideIDType remoteSideID)
        {
        }

        protected virtual void KnownRemoteSideRequest(RemoteSideIDType remoteSideID)
        {
        }

        protected virtual void RemoteSideReconnected(RemoteSideIDType remoteSideID)
        {
        }

        protected virtual void RemoteRequestStarted(RemoteSideIDType remoteSideID, RemoteRequest request)
        {
        }

        protected virtual void RemoteRequestFinished(RemoteSideIDType remoteSideID, RemoteRequest request)
        {
        }

        public virtual RemoteResponse ExecuteRequest(RemoteRequest request)
		{
            ThrowIfNotInitialized();

            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            RemoteResponse ret = null;

            RemoteSideIDType remoteID = request.RemoteID;
            if (remoteID == null)
            {
                throw new InvalidOperationException("Remote call without remote side id!"); //LOCSTR
            }

            var wrapper = remoteSide;
            IRemoteSideCommunicationContract currentContract = wrapper.GetCurrentRemoteSideCommunicationContract();

            try
            {
                bool isNewRemoteSide = false;
                bool lockTaken = false;
                try
                {
                    syncRoot.Enter(ref lockTaken);

                    RemoteRequestStarted(remoteID, request);

                    IRemoteSideCommunicationContract lastKnownContract = null;
                    if (communicationContractsByRemoteSideIDDict.TryGetValue(remoteID, out lastKnownContract))
                    {
                        // known client comes in again
                        if (lastKnownContract != currentContract)
                        {
                            communicationContractsByRemoteSideIDDict[remoteID] = currentContract;
                            remoteSideIDsByCommunicationContractDict.Remove(lastKnownContract);
                            remoteSideIDsByCommunicationContractDict.Add(currentContract, remoteID);

                            wrapper.Closed += new EventHandler(Channel_Closed);
                            wrapper.Faulted += new EventHandler(Channel_Faulted);

                            RemoteSideReconnected(remoteID);
                            //Log.Debug("Client {0} reconnected.", remoteID.ToString()); //LOCSTR
                            System.Diagnostics.Debug.WriteLine("Client {0} reconnected.", remoteID.ToString()); //LOCSTR
                        }

                        KnownRemoteSideRequest(remoteID);
                    }
                    else
                    {
                        wrapper.Closed += new EventHandler(Channel_Closed);
                        wrapper.Faulted += new EventHandler(Channel_Faulted);

                        remoteSideIDsByCommunicationContractDict.Add(currentContract, remoteID);
                        communicationContractsByRemoteSideIDDict.Add(remoteID, currentContract);

                        NewRemoteSideConnected(remoteID);

                        isNewRemoteSide = true;
                    }
                }
                finally
                {
                    if (lockTaken)
                    {
                        syncRoot.Exit();
                    }
                }

                if (isNewRemoteSide)
                {
                    OnRemoteSideConnected(remoteID);
                }

                // process request
                ret = remoteOperationHandler.ExecuteRequest(request, typeof(RemoteCallableTypeAttribute), typeof(RemoteCallableFuncAttribute));
            }
            finally
            {
                RemoteRequestFinished(remoteID, request);
            }
        
            return ret;
        }

        private void Channel_Closed(object sender, EventArgs e)
        {
            IRemoteSideCommunicationContract sc = sender as IRemoteSideCommunicationContract;
            bool lockTaken = false;
            try
            {
                syncRoot.Enter(ref lockTaken);
                RemoteSideIDType remoteSideID;

                if (remoteSideIDsByCommunicationContractDict.TryGetValue(sc, out remoteSideID))
                {
                    DisconnectRemoteSide(remoteSideID, sc);
                }
            }
            finally
            {
                if (lockTaken)
                {
                    syncRoot.Exit();
                }
            }
        }

        protected virtual void RemoteSideFaulted(RemoteSideIDType remoteID)
        {
        }

        private void Channel_Faulted(object sender, EventArgs e)
        {
            IRemoteSideCommunicationContract sc = sender as IRemoteSideCommunicationContract;
            bool lockTaken = false;
            try
            {
                syncRoot.Enter(ref lockTaken);
                RemoteSideIDType remoteSideID;

                if (remoteSideIDsByCommunicationContractDict.TryGetValue(sc, out remoteSideID))
                {
                    RemoteSideFaulted(remoteSideID);
                }
                else
                {
                    throw new InvalidOperationException(string.Format("Unkwon ID: {0}!", remoteSideID));
                }
            }
            finally
            {
                if (lockTaken)
                {
                    syncRoot.Exit();
                }
            }
        }


        public event EventHandler<RemoteSideConnectedEventArgs> RemoteSideConnected;
        private void OnRemoteSideConnected(RemoteSideIDType remoteSideID)
        {
            System.Diagnostics.Debug.WriteLine("New connection: {0}!", remoteSideID.ToString()); 

            if (RemoteSideConnected != null)
            {
                RemoteSideConnected(this, new RemoteSideConnectedEventArgs(remoteSideID));
            }
        }

        public event EventHandler<RemoteSideDisconnectedEventArgs> RemoteSideDisconnected;
        private void OnRemoteSideDisconnected(RemoteSideIDType remoteSideID)
        {
            if (RemoteSideDisconnected != null)
            {
                RemoteSideDisconnected(this, new RemoteSideDisconnectedEventArgs(remoteSideID));
            }
        }

        public TResult ExecuteOnRemoteSide<TResult>(RemoteSideIDType remoteSideID, RemoteOperationDescriptor rso)
        {
            ThrowIfNotInitialized();
            TResult ret = (TResult)ExecuteOnRemoteSideInternal(remoteSideID, rso);
            return ret;
        }

        public void ExecuteOnRemoteSide(RemoteSideIDType remoteSideID, RemoteOperationDescriptor rso)
        {
            ThrowIfNotInitialized();
            ExecuteOnRemoteSideInternal(remoteSideID, rso);
        }

        protected virtual void RequestToRemoteSideStarted(RemoteSideIDType remoteSideID, RemoteOperationDescriptor rso)
        {
        }

        protected virtual void RequestToRemoteSideFinished(RemoteSideIDType remoteSideID, RemoteOperationDescriptor rso)
        {
        }

        protected virtual object ExecuteOnRemoteSideInternal(RemoteSideIDType remoteSideID, RemoteOperationDescriptor rso)
        {
            object ret = null;

            if (remoteSideID == null)
            {
                throw new ArgumentNullException("remoteSideID");
            }

            if (rso == null)
            {
                throw new ArgumentNullException("rso");
            }

            // find appropriate client
            IRemoteSideCommunicationContract contract = null;
            bool lockTaken = false;
            try
            {
                syncRoot.Enter(ref lockTaken);
                communicationContractsByRemoteSideIDDict.TryGetValue(remoteSideID, out contract);
            }
            finally
            {
                if (lockTaken)
                {
                    syncRoot.Exit();
                }
            }

            if (contract != null)
            {
                RemoteRequest req = remoteOperationHandler.CreateRequest(rso);
                req.RemoteID = remoteSideID;

                RemoteResponse resp = null;

                try
                {
                    RequestToRemoteSideStarted(remoteSideID, rso);

                    resp = contract.ExecuteRequest(req);
                }
                finally
                {
                    RequestToRemoteSideFinished(remoteSideID, rso);
                }
                ret = remoteOperationHandler.HandleResponse(resp);
            }
            else
            {
                throw new InvalidOperationException(String.Format("Unknown remote side id: {0}", remoteSideID)); 
            }

            return ret;
        }

        public virtual void AssignRemoteSide(IRemoteSide remoteSide)
        {
            if (remoteSide == null)
            {
                throw new ArgumentNullException("clientServiceHostInstance");
            }

            this.remoteSide = remoteSide;
            this.remoteSide.StateChanged += new EventHandler(remoteSide_StateChanged);

            communicationContractsByRemoteSideIDDict.Add(remoteSide.ID, (IRemoteSideCommunicationContract)remoteSide);
            remoteSideIDsByCommunicationContractDict.Add((IRemoteSideCommunicationContract)remoteSide, remoteSide.ID);
        }

        private void remoteSide_StateChanged(object sender, EventArgs args)
        {
            if (sender == remoteSide)
            {
                var state = remoteSide.State;
                HandleStateChanged(state);
            }
        }

        private void ThrowIfNotInitialized()
        {
            if (remoteSide == null)
            {
                throw new InvalidOperationException("ClientService not initialized yet!"); 
            }
        }

        protected virtual void HandleStateChanged(RemoteCommunicationState state)
        {
            if (state == RemoteCommunicationState.Faulted)
            {
                try
                {
                    remoteSide.Abort();
                }
                catch { } // dont care
            }
        }
    }
}
