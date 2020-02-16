using System.Collections.Generic;

namespace Cogito.Kademlia.Network
{

    /// <summary>
    /// Describes a Kademlia IP network peer.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public class KIpPeer<TKNodeId> : IKIpEndpointProvider
        where TKNodeId : IKNodeId<TKNodeId>
    {

        readonly TKNodeId nodeId;
        readonly List<KIpEndpoint> endpoints = new List<KIpEndpoint>();

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public KIpPeer(TKNodeId nodeId)
        {
            this.nodeId = nodeId;
        }

        /// <summary>
        /// Gets the endpoints associated with the peer.
        /// </summary>
        public IList<KIpEndpoint> Endpoints => endpoints;

    }

}
