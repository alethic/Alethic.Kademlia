using System;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Provides required operations for node communication within Kademlia.
    /// </summary>
    public interface IKProtocol<TNodeId>
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Attempts to create an endpoint for the protocol given the specified URI. Can return <c>null</c> if the protocol does not support the endpoint.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        IKProtocolEndpoint<TNodeId> ResolveEndpoint(Uri uri);

    }

}
