using System;
using System.Collections.Generic;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Represents a result from a lookup operation against a node.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KLookupNodeResult<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly TKNodeId key;
        readonly IEnumerable<KPeerEndpointInfo<TKNodeId>> nodes;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="nodes"></param>
        public KLookupNodeResult(in TKNodeId key, IEnumerable<KPeerEndpointInfo<TKNodeId>> nodes)
        {
            this.key = key;
            this.nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
        }

        /// <summary>
        /// Gets the key that resulted in this lookup result.
        /// </summary>
        public TKNodeId Key => key;

        /// <summary>
        /// Gets the set of nodes and node endpoints discovered on the way to the key, sorted by distance.
        /// </summary>
        public IEnumerable<KPeerEndpointInfo<TKNodeId>> Nodes => nodes;

    }

}
