using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Reflection;
using ArdinHelpers;
using ArdinRemoteOperations;

namespace ArdinRemoteOperations.WCF
{
public class WCFServiceClient : DuplexClientBase<IRemoteSideCommunicationContract>, IRemoteSide, IRemoteSideCommunicationContract
{
    public WCFServiceClient(InstanceContext instanceContext, Binding binding, EndpointAddress remoteAddress) :
        base(instanceContext, binding, remoteAddress)
    {
        this.ID = RemoteSideIDType.Parse(Guid.NewGuid().ToString());

        ((ICommunicationObject)base.Channel).Closed += new EventHandler(ClientServiceContractClient_StateChanged);
        ((ICommunicationObject)base.Channel).Closing += new EventHandler(ClientServiceContractClient_StateChanged);
        ((ICommunicationObject)base.Channel).Faulted += new EventHandler(ClientServiceContractClient_StateChanged);
        ((ICommunicationObject)base.Channel).Opened += new EventHandler(ClientServiceContractClient_StateChanged);
        ((ICommunicationObject)base.Channel).Opening += new EventHandler(ClientServiceContractClient_StateChanged);
    }

    public event EventHandler StateChanged;

    public new RemoteCommunicationState State
    {
        get
        {
            RemoteCommunicationState ret = (RemoteCommunicationState)Enum.Parse(typeof(RemoteCommunicationState), base.State.ToString());
            return ret;
        }
    }

    private void OnStateChanged()
    {
        if (this.StateChanged != null)
        {
            StateChanged(this, EventArgs.Empty);
        }
    }

    private void ClientServiceContractClient_StateChanged(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("WCFServiceClient.State changed: {0}", ((ICommunicationObject)base.Channel).State.ToString());
        OnStateChanged();
    }


    public RemoteResponse ExecuteRequest(RemoteRequest oRequest)
    {
        RemoteResponse ret = null;
        try
        {
            try
            {
                ret = base.Channel.ExecuteRequest(oRequest);
            }
            catch (FaultException<ExceptionDetail> ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());

                // extract & throw original exception from Fault contract
                Exception originalException = null;
                try
                {
                    originalException = (Exception)Activator.CreateInstance(Type.GetType(ex.Detail.Type), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new object[] { ex.Message, ex }, null);
                }
                catch
                {
                    throw;
                }
                throw originalException;
            }
        }
        catch (CommunicationObjectFaultedException ex)
        {
            // wrap WCF specific exception
            throw new RemoteSideFaultedException(ex.Message, ex);
        }
        catch (EndpointNotFoundException ex)
        {
            // wrap WCF specific exception
            throw new RemoteSideUnreachableException(ex.Message, ex);
        }
        return ret;
    }


    public event EventHandler Closed
    {
        add
        {
            ((ICommunicationObject)this).Closed += value;
        }
        remove 
        {
            ((ICommunicationObject)this).Closed -= value;
        }
    }

    public event EventHandler Faulted
    {
        add
        {
            ((ICommunicationObject)this).Faulted += value;
        }
        remove
        {
            ((ICommunicationObject)this).Faulted -= value;
        }
    }        
    public IRemoteSideCommunicationContract GetCurrentRemoteSideCommunicationContract()
    {
        IRemoteSideCommunicationContract ret = this;
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
