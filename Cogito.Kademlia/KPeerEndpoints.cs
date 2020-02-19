using System.Collections.Generic;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a peer and its associated endpoints.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    /// <typeparam name="TKPeerData"></typeparam>
    public readonly struct KPeerEndpoints<TKNodeId>
        where TKNodeId :  unmanaged, IKNodeId<TKNodeId>
    {

        readonly TKNodeId id;
        readonly IEnumerable<IKEndpoint<TKNodeId>> endpoints;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="endpoints"></param>
        public KPeerEndpoints(in TKNodeId id, IEnumerable<IKEndpoint<TKNodeId>> endpoints)
        {
            this.id = id;
            this.endpoints = endpoints;
        }

        /// <summary>
        /// Gets the node ID of the peer.
        /// </summary>
        public TKNodeId Id => id;

        /// <summary>
        /// Gets the set of known endpoints of the peer.
        /// </summary>
        public IEnumerable<IKEndpoint<TKNodeId>> Endpoints => endpoints;

    }

}
