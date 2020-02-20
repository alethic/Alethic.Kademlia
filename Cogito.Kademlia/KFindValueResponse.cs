using System;
using System.Collections.Generic;
using System.Linq;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a response to a FIND_VALUE request.
    /// </summary>
    public readonly struct KFindValueResponse<TKNodeId> : IKResponseData<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly TKNodeId key;
        readonly ReadOnlyMemory<byte>? value;
        readonly KPeerEndpointInfo<TKNodeId>[] peers;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="peers"></param>
        public KFindValueResponse(in TKNodeId key, ReadOnlyMemory<byte>? value, KPeerEndpointInfo<TKNodeId>[] peers)
        {
            this.key = key;
            this.value = value;
            this.peers = peers;
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="peers"></param>
        public KFindValueResponse(in TKNodeId key, ReadOnlyMemory<byte>? value, IEnumerable<KPeerEndpointInfo<TKNodeId>> peers) :
            this(key, value, peers.ToArray())
        {

        }

        /// <summary>
        /// Gets the key to locate the value of.
        /// </summary>
        public TKNodeId Key => key;

        /// <summary>
        /// Gets the value that was located.
        /// </summary>
        public ReadOnlyMemory<byte>? Value => value;

        /// <summary>
        /// Gets the set of peers and their endpoints returned by the lookup.
        /// </summary>
        public KPeerEndpointInfo<TKNodeId>[] Peers => peers;

    }

}
