using System;
using System.Buffers;
using System.Buffers.Binary;

namespace Cogito.Kademlia.Network.Datagram
{

    /// <summary>
    /// Describes a STORE request.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly ref struct KPacketStoreRequest<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Gets the size of the packet given the inputs.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static void Write(IBufferWriter<byte> writer, KStoreRequest<TKNodeId> request)
        {
            KNodeId<TKNodeId>.Write(request.Key, writer);

            BinaryPrimitives.WriteUInt32BigEndian(writer.GetSpan(sizeof(uint)), (uint)request.Value.Length);
            writer.Advance(sizeof(uint));

            writer.Write(request.Value.Span);
        }

        readonly ReadOnlySpan<byte> span;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="span"></param>
        public KPacketStoreRequest(ReadOnlySpan<byte> span)
        {
            this.span = span;
        }

        /// <summary>
        /// Gets the key being stored.
        /// </summary>
        public TKNodeId Key
        {
            get => KNodeId<TKNodeId>.Read(span.Slice(KPacketStoreRequestInfo<TKNodeId>.KeyOffset, KPacketStoreRequestInfo<TKNodeId>.KeySize));
        }

        /// <summary>
        /// Gets the size of the value being stored.
        /// </summary>
        public int ValueSize
        {
            get => (int)BinaryPrimitives.ReadUInt32BigEndian(span.Slice(KPacketStoreRequestInfo<TKNodeId>.ValueCountOffset, KPacketStoreRequestInfo<TKNodeId>.ValueCountSize));
        }

        /// <summary>
        /// Gets the value being stored.
        /// </summary>
        public ReadOnlySpan<byte> Value
        {
            get => span.Slice(KPacketStoreRequestInfo<TKNodeId>.ValueOffset, ValueSize);
        }
    }

}
