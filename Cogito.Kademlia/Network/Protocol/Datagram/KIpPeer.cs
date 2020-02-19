using System;

namespace Cogito.Kademlia.Network.Protocol.Datagram
{

    /// <summary>
    /// Describes a peer and it's associated IP endpoints.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KIpPeer<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly TKNodeId nodeId;
        readonly ReadOnlyMemory<KIpEndpoint> endpoints;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="endpoints"></param>
        public KIpPeer(in TKNodeId nodeId, ReadOnlyMemory<KIpEndpoint> endpoints)
        {
            this.nodeId = nodeId;
            this.endpoints = endpoints;
        }

        /// <summary>
        /// Gets the node ID of the peer.
        /// </summary>
        public TKNodeId NodeId => nodeId;

        /// <summary>
        /// Gets the known IP endpoints of the peer.
        /// </summary>
        public ReadOnlyMemory<KIpEndpoint> Endpoints => endpoints;

    }

}
