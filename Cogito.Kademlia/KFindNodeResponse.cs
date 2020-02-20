using System.Collections.Generic;
using System.Linq;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a response to a FIND_NODE request.
    /// </summary>
    public readonly struct KFindNodeResponse<TKNodeId> : IKResponseData<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly TKNodeId key;
        readonly KPeerEndpointInfo<TKNodeId>[] peers;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="peers"></param>
        public KFindNodeResponse(in TKNodeId key, KPeerEndpointInfo<TKNodeId>[] peers)
        {
            this.key = key;
            this.peers = peers;
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="peers"></param>
        public KFindNodeResponse(in TKNodeId key, IEnumerable<KPeerEndpointInfo<TKNodeId>> peers) :
            this(key, peers.ToArray())
        {

        }

        /// <summary>
        /// Gets the node ID that was searched for.
        /// </summary>
        public TKNodeId Key => key;

        /// <summary>
        /// Gets the set of peers and their endpoints returned by the lookup.
        /// </summary>
        public KPeerEndpointInfo<TKNodeId>[] Peers => peers;

    }

}
