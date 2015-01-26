using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace ArdinRemoteOperations
{
    /// <summary>
    /// Interface which describe methods to generate request from RemoteSideOperation and handle responses.
    /// </summary>
    public interface IRemoteOperationHandler
    {
        /// <summary>
        /// Creates the request object from the RemoteSideOperation
        /// </summary>
        /// <param name="rso">Describes the request parameters</param>
        /// <returns>Created request object</returns>
        RemoteRequest CreateRequest(RemoteOperationDescriptor rso);

        /// <summary>
        /// Executes the request on the remote side.
        /// </summary>
        /// <param name="request">Request</param>
        /// <param name="callableTypeAttribute"></param>
        /// <param name="callableFuncAttribute"></param>
        /// <returns>Response</returns>
        RemoteResponse ExecuteRequest(RemoteRequest request, Type callableTypeAttribute, Type callableFuncAttribute);

        /// <summary>
        /// Handles the response of the remote side request
        /// </summary>
        /// <param name="resp">Response object</param>
        /// <returns>Value stored in response</returns>
        object HandleResponse(RemoteResponse resp);
    }
}
