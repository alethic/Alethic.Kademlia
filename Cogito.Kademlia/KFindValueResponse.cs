﻿using System;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a response to a FIND_VALUE request.
    /// </summary>
    public readonly struct KFindValueResponse<TKNodeId> : IKResponseData<TKNodeId>, IKMessageBody<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly TKNodeId key;
        readonly KPeerEndpointInfo<TKNodeId>[] peers;
        readonly ReadOnlyMemory<byte>? value;
        readonly DateTimeOffset? expiration;
        readonly ulong? version;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="peers"></param>
        /// <param name="value"></param>
        /// <param name="expiration"></param>
        /// <param name="version"></param>
        public KFindValueResponse(in TKNodeId key, KPeerEndpointInfo<TKNodeId>[] peers, ReadOnlyMemory<byte>? value, DateTimeOffset? expiration, ulong? version)
        {
            this.key = key;
            this.peers = peers;
            this.value = value;
            this.expiration = expiration ?? throw new ArgumentNullException(nameof(expiration));
            this.version = version;
        }

        /// <summary>
        /// Gets the key to locate the value of.
        /// </summary>
        public TKNodeId Key => key;

        /// <summary>
        /// Gets the set of peers and their endpoints returned by the lookup.
        /// </summary>
        public KPeerEndpointInfo<TKNodeId>[] Peers => peers;

        /// <summary>
        /// Gets the value that was located.
        /// </summary>
        public ReadOnlyMemory<byte>? Value => value;

        /// <summary>
        /// Gets the date and time at which the value expires.
        /// </summary>
        public DateTimeOffset? Expiration => expiration;

        /// <summary>
        /// Gets the version of the retrieved value.
        /// </summary>
        public ulong? Version => version;

    }

}
