using System;
using System.Buffers;
using System.Buffers.Binary;

using Cogito.Kademlia.Core;

namespace Cogito.Kademlia.Network.Datagram
{

    /// <summary>
    /// Describes a FIND_VALUE response.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly ref struct KPacketFindValueResponse<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        public static void Write(ArrayBufferWriter<byte> writer, KFindValueResponse<TKNodeId> response)
        {
            KNodeId<TKNodeId>.Write(response.Key, writer);

            BinaryPrimitives.WriteInt32BigEndian(writer.GetSpan(sizeof(int)), response.Value?.Length ?? -1);
            if (response.Value != null)
                writer.Write(response.Value.Value.Span);

            KPacketPeerSequence<TKNodeId>.Write(writer, response.Peers);
        }

        readonly ReadOnlySpan<byte> span;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="span"></param>
        public KPacketFindValueResponse(ReadOnlySpan<byte> span)
        {
            this.span = span;
        }

        /// <summary>
        /// Gets the key of the node being searched for.
        /// </summary>
        public TKNodeId Key
        {
            get => KNodeId<TKNodeId>.Read(span.Slice(KPacketFindValueResponseInfo<TKNodeId>.KeyOffset, KPacketFindValueResponseInfo<TKNodeId>.KeySize));
        }

        /// <summary>
        /// Gets the size of the value, or -1 if no value is set.
        /// </summary>
        public int ValueSize
        {
            get => BinaryPrimitives.ReadInt32BigEndian(span.Slice(KPacketFindValueResponseInfo<TKNodeId>.ValueSizeOffset, KPacketFindValueResponseInfo<TKNodeId>.ValueSizeSize));
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public ReadOnlySpan<byte> Value
        {
            get => ValueSize > -1 ? span.Slice(KPacketFindValueResponseInfo<TKNodeId>.ValueOffset, ValueSize) : throw new InvalidOperationException();
        }

        /// <summary>
        /// 
        /// </summary>
        public KPacketPeerSequence<TKNodeId> Peers
        {
            get => new KPacketPeerSequence<TKNodeId>(span.Slice(KPacketFindValueResponseInfo<TKNodeId>.ValueOffset + ValueSize));
        }

    }

}
