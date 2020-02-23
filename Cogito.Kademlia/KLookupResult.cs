using System;
using System.Collections.Generic;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Represents a result from a lookup operation against a node.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public struct KLookupResult<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly TKNodeId key;
        readonly KPeerEndpointInfo<TKNodeId>? final;
        readonly IEnumerable<KPeerEndpointInfo<TKNodeId>> nodes;
        readonly ReadOnlyMemory<byte>? value;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="final"></param>
        /// <param name="nodes"></param>
        /// <param name="value"></param>
        public KLookupResult(in TKNodeId key, in KPeerEndpointInfo<TKNodeId>? final, IEnumerable<KPeerEndpointInfo<TKNodeId>> nodes, ReadOnlyMemory<byte>? value)
        {
            this.key = key;
            this.final = final;
            this.nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            this.value = value;
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="final"></param>
        /// <param name="nodes"></param>
        public KLookupResult(in TKNodeId key, in KPeerEndpointInfo<TKNodeId>? final, IEnumerable<KPeerEndpointInfo<TKNodeId>> nodes) :
            this(key, final, nodes, null)
        {

        }

        /// <summary>
        /// Gets the key that resulted in this lookup result.
        /// </summary>
        public TKNodeId Key => key;

        /// <summary>
        /// Gets the node ID that terminated the lookup.
        /// </summary>
        public KPeerEndpointInfo<TKNodeId>? Final => final;

        /// <summary>
        /// Gets the set of nodes and node endpoints discovered on the way to the key, sorted by distance.
        /// </summary>
        public IEnumerable<KPeerEndpointInfo<TKNodeId>> Nodes => nodes;

        /// <summary>
        /// Gets the resulting value if any.
        /// </summary>
        public ReadOnlyMemory<byte>? Value => value;

    }

}
