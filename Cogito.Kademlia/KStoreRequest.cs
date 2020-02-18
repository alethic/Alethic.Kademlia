using System;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a STORE request.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KStoreRequest<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly TKNodeId key;
        readonly ReadOnlyMemory<byte> value;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public KStoreRequest(in TKNodeId key, ReadOnlyMemory<byte> value)
        {
            this.key = key;
            this.value = value;
        }

        /// <summary>
        /// Specifies the key to be stored.
        /// </summary>
        public TKNodeId Key => key;

        /// <summary>
        /// Specifies the value to be stored with the key.
        /// </summary>
        public ReadOnlyMemory<byte> Value => value;

    }

}
