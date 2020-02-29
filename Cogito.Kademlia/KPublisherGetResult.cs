﻿using System;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Descsribes the results of a publisher get operation.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KPublisherGetResult<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly TKNodeId key;
        readonly ReadOnlyMemory<byte>? value;
        readonly DateTimeOffset? expiration;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="expiration"></param>
        public KPublisherGetResult(in TKNodeId key, ReadOnlyMemory<byte>? value, DateTimeOffset? expiration)
        {
            this.key = key;
            this.value = value;
            this.expiration = expiration;
        }

        /// <summary>
        /// Gets the key that is associated with the retrieved value.
        /// </summary>
        public TKNodeId Key => key;

        /// <summary>
        /// Gets the value retrieved from the store. If no value exists, <c>null</c> is returned.
        /// </summary>
        public ReadOnlyMemory<byte>? Value => value;

        /// <summary>
        /// Gets the time at which the value will expire, if a value with expiration exists.
        /// </summary>
        public DateTimeOffset? Expiration => expiration;

    }

}
