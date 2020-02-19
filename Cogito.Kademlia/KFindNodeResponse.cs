using System.Collections.Generic;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a response to a FIND_NODE request.
    /// </summary>
    public readonly struct KFindNodeResponse<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly TKNodeId key;
        readonly IEnumerable<KPeerEndpoints<TKNodeId>> endpoints;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="endpoints"></param>
        public KFindNodeResponse(in TKNodeId key, IEnumerable<KPeerEndpoints<TKNodeId>> endpoints)
        {
            this.key = key;
            this.endpoints = endpoints;
        }

        /// <summary>
        /// Gets the node ID that was searched for.
        /// </summary>
        public TKNodeId Key => key;

        /// <summary>
        /// Gets the set of endpoints returned by the lookup.
        /// </summary>
        public IEnumerable<KPeerEndpoints<TKNodeId>> Endpoints => endpoints;

    }

}
