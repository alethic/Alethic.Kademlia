using System;
using System.Collections.Generic;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Represents a result from a lookup operation for a value.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KLookupValueResult<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly TKNodeId key;
        readonly IEnumerable<KPeerEndpointInfo<TKNodeId>> nodes;
        readonly KPeerEndpointInfo<TKNodeId>? source;
        readonly ReadOnlyMemory<byte>? value;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="nodes"></param>
        /// <param name="source"></param>
        /// <param name="value"></param>
        public KLookupValueResult(in TKNodeId key, IEnumerable<KPeerEndpointInfo<TKNodeId>> nodes, in KPeerEndpointInfo<TKNodeId>? source, ReadOnlyMemory<byte>? value)
        {
            this.key = key;
            this.nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            this.source = source;
            this.value = value;
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="nodes"></param>
        /// <param name="final"></param>
        public KLookupValueResult(in TKNodeId key, IEnumerable<KPeerEndpointInfo<TKNodeId>> nodes, in KPeerEndpointInfo<TKNodeId>? final) :
            this(key, nodes, final, null)
        {

        }

        /// <summary>
        /// Gets the key that resulted in this lookup result.
        /// </summary>
        public TKNodeId Key => key;

        /// <summary>
        /// Gets the set of nodes and node endpoints discovered on the way to the key, sorted by distance.
        /// </summary>
        public IEnumerable<KPeerEndpointInfo<TKNodeId>> Nodes => nodes;

        /// <summary>
        /// Gets the node information that returned the value.
        /// </summary>
        public KPeerEndpointInfo<TKNodeId>? Source => source;

        /// <summary>
        /// Gets the resulting value if any.
        /// </summary>
        public ReadOnlyMemory<byte>? Value => value;

    }

}
