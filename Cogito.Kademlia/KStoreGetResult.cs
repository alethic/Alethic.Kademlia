using System;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Descsribes the results of a store get operation.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KStoreGetResult<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly TKNodeId key;
        readonly ReadOnlyMemory<byte>? value;
        readonly DateTimeOffset? expiration;
        readonly ulong? version;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiration"></param>
        /// <param name="version"></param>
        public KStoreGetResult(in TKNodeId key, ReadOnlyMemory<byte>? value, DateTimeOffset? expiration, ulong? version)
        {
            this.key = key;
            this.value = value;
            this.expiration = expiration;
            this.version = version;
        }

        /// <summary>
        /// Gets the key that was retrieved.
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

        /// <summary>
        /// Gets the version of the value.
        /// </summary>
        public ulong? Version => version;

    }

}
