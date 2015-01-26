using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace ArdinRemoteOperations
{
    /// <summary>
    /// Describes a communicator object to start remote calls and handles states
    /// </summary>
    public interface IRemoteSideCommunicationHandler
    {
        /// <summary>
        /// Executes a request on the remote side and gives the return back
        /// </summary>
        /// <typeparam name="TResult">Return type of the remote call</typeparam>
        /// <param name="remoteSideID">Remote client ID</param>
        /// <param name="rso">The descriptor of the operation</param>
        /// <returns>Result</returns>
        TResult ExecuteOnRemoteSide<TResult>(RemoteSideIDType remoteSideID, RemoteOperationDescriptor rso);

        /// <summary>
        /// Executes a request on the remote side
        /// </summary>
        /// <param name="remoteSideID">Remote client ID</param>
        /// <param name="rso">The descriptor of the operation</param>
        void ExecuteOnRemoteSide(RemoteSideIDType remoteSideID, RemoteOperationDescriptor rso);

        /// <summary>
        /// The id of the remote client whos request is executed currently
        /// </summary>
        RemoteSideIDType CurrentRemoteSideID { get; }

        /// <summary>
        /// Remote client connected event
        /// </summary>
        event EventHandler<RemoteSideConnectedEventArgs> RemoteSideConnected;

        /// <summary>
        /// Remote client disconnected event
        /// </summary>
        event EventHandler<RemoteSideDisconnectedEventArgs> RemoteSideDisconnected;

        /// <summary>
        /// Assign the concrete ClientServiceHost
        /// </summary>
        /// <param name="remoteSide">Service host</param>
        void AssignRemoteSide(IRemoteSide remoteSide);
    }
}
