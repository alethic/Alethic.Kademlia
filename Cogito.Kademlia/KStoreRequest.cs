using System;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a STORE request.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KStoreRequest<TKNodeId> : IKMessageBody<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Creates a response to the given request.
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public KStoreResponse<TKNodeId> Respond(KStoreResponseStatus status)
        {
            return new KStoreResponse<TKNodeId>(key, status);
        }

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
        public KStoreRequest(in TKNodeId key, ReadOnlyMemory<byte>? value, DateTimeOffset? expiration, ulong? version)
        {
            this.key = key;
            this.value = value;
            this.expiration = expiration;
            this.version = version;
        }

        /// <summary>
        /// Specifies the key to be stored.
        /// </summary>
        public TKNodeId Key => key;

        /// <summary>
        /// Specifies the value to be stored with the key.
        /// </summary>
        public ReadOnlyMemory<byte>? Value => value;

        /// <summary>
        /// Time at which the value will expire.
        /// </summary>
        public DateTimeOffset? Expiration => expiration;

        /// <summary>
        /// Version of the data to store. Later version override older versions.
        /// </summary>
        public ulong? Version => version;

    }

}
