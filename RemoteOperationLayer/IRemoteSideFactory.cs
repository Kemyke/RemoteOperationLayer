using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArdinRemoteOperations
{
    /// <summary>
    /// Factory which creates a new IRemoteSide
    /// </summary>
    public interface IRemoteSideFactory
    {
        /// <summary>
        /// Creates a new remote side instance
        /// </summary>
        /// <returns>New instance</returns>
        IRemoteSide CreateInstance();
    }
}
