using System;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Represents a Kademlia network engine.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public interface IKEngine<TNodeId>
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Gets the Node ID of the node itself.
        /// </summary>
        TNodeId SelfId { get; }

        /// <summary>
        /// Gets the set of endpoints of the node.
        /// </summary>
        KEndpointSet<TNodeId> Endpoints { get; }

        /// <summary>
        /// Resolves the protocol endpoint from the given URI.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        IKProtocolEndpoint<TNodeId> ResolveEndpoint(Uri uri);

    }

}
