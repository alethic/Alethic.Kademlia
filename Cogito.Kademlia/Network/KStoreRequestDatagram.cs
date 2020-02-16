using System;

namespace Cogito.Kademlia.Network
{

    /// <summary>
    /// Describes the data to send for a ping request.
    /// </summary>
    public struct KStoreRequestDatagram<TKNodeId> : IKRequestDatagram
        where TKNodeId : struct, IKNodeId<TKNodeId>
    {

        readonly uint magic;
        readonly TKNodeId key;
        readonly ReadOnlyMemory<byte> value;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="magic"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public KStoreRequestDatagram(uint magic, TKNodeId key, ReadOnlyMemory<byte> value)
        {
            this.magic = magic;
            this.key = key;
            this.value = value;
        }

        public uint Magic => magic;

        public TKNodeId Key => key;

        public ReadOnlySpan<byte> Value => value.Span;

    }

}
