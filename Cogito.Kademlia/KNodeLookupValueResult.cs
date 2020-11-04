using System;
using System.Collections.Generic;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Represents a result from a lookup operation for a value.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public readonly struct KNodeLookupValueResult<TNodeId>
        where TNodeId : unmanaged
    {

        readonly TNodeId key;
        readonly IEnumerable<KPeerEndpointInfo<TNodeId>> nodes;
        readonly KPeerEndpointInfo<TNodeId>? source;
        readonly KValueInfo? value;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="nodes"></param>
        /// <param name="source"></param>
        /// <param name="value"></param>
        public KNodeLookupValueResult(in TNodeId key, IEnumerable<KPeerEndpointInfo<TNodeId>> nodes, in KPeerEndpointInfo<TNodeId>? source, in KValueInfo? value)
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
        public KNodeLookupValueResult(in TNodeId key, IEnumerable<KPeerEndpointInfo<TNodeId>> nodes, in KPeerEndpointInfo<TNodeId>? final) :
            this(key, nodes, final, null)
        {

        }

        /// <summary>
        /// Gets the key that resulted in this lookup result.
        /// </summary>
        public TNodeId Key => key;

        /// <summary>
        /// Gets the set of nodes and node endpoints discovered on the way to the key, sorted by distance.
        /// </summary>
        public IEnumerable<KPeerEndpointInfo<TNodeId>> Nodes => nodes;

        /// <summary>
        /// Gets the node information that returned the value.
        /// </summary>
        public KPeerEndpointInfo<TNodeId>? Source => source;

        /// <summary>
        /// Gets the resulting value if any.
        /// </summary>
        public KValueInfo? Value => value;

    }

}
