using System;
using System.Linq.Expressions;
using System.ServiceModel;

namespace ArdinRemoteOperations
{
    /// <summary>
    /// Interface which contains the remote callable methods
    /// </summary>
	[ServiceContract(CallbackContract=typeof(IRemoteSideCommunicationContract))]
	public interface IRemoteSideCommunicationContract
	{
        /// <summary>
        /// Remote callable method which handles all the requests from the remote side
        /// </summary>
		[OperationContract]
        RemoteResponse ExecuteRequest(RemoteRequest request);
	}
}
