using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArdinRemoteOperations
{
    /// <summary>
    /// Represent a remote side communication endpoint
    /// </summary>
    public interface IRemoteSide
    {
        /// <summary>
        /// Raised after the communication is closed
        /// </summary>
        event EventHandler Closed;
        /// <summary>
        /// Raised after the communication is faulted
        /// </summary>
        event EventHandler Faulted;
        /// <summary>
        /// State of the communication
        /// </summary>
        RemoteCommunicationState State { get; }
        /// <summary>
        /// Raised after the state changed
        /// </summary>
        event EventHandler StateChanged;
        /// <summary>
        /// Open the communication
        /// </summary>
        void Open();
        /// <summary>
        /// Close the communication 
        /// </summary>
        void Close();
        /// <summary>
        /// Abort the communication
        /// </summary>
        void Abort();
        /// <summary>
        /// Concrete communication contract implementation
        /// </summary>
        /// <returns></returns>
        IRemoteSideCommunicationContract GetCurrentRemoteSideCommunicationContract();

        RemoteSideIDType ID { get; }
    }
}
