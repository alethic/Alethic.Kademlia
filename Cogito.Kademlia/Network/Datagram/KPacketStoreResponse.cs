using System;
using System.Buffers;

using Cogito.Kademlia.Core;

namespace Cogito.Kademlia.Network.Datagram
{

    /// <summary>
    /// Describes a STORE response.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly ref struct KPacketStoreResponse<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        public static void Write(ArrayBufferWriter<byte> writer, KStoreResponse<TKNodeId> response)
        {
            KNodeId<TKNodeId>.Write(response.Key, writer);
        }

        readonly ReadOnlySpan<byte> span;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="span"></param>
        public KPacketStoreResponse(ReadOnlySpan<byte> span)
        {
            this.span = span;
        }

        /// <summary>
        /// Gets the key of the value that was stored.
        /// </summary>
        public TKNodeId Key
        {
            get => KNodeId<TKNodeId>.Read(span.Slice(KPacketStoreResponseInfo<TKNodeId>.KeyOffset, KPacketStoreResponseInfo<TKNodeId>.KeySize));
        }
    }

}
