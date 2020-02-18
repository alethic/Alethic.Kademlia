using System;

namespace Cogito.Kademlia.Network.Protocol.Datagram
{

    /// <summary>
    /// Describes the data to send for a STORE request.
    /// </summary>
    public readonly ref struct KStoreRequestBody<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly TKNodeId key;
        readonly ReadOnlySpan<byte> value;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public KStoreRequestBody(in TKNodeId key, ReadOnlySpan<byte> value)
        {
            this.key = key;
            this.value = value;
        }

        /// <summary>
        /// Gets the key to store.
        /// </summary>
        public TKNodeId Key => key;

        /// <summary>
        /// Gets the value to store.
        /// </summary>
        public ReadOnlySpan<byte> Value => value;

    }

}
