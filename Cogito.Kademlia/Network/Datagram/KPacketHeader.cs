using System;
using System.Buffers;
using System.Buffers.Binary;

namespace Cogito.Kademlia.Network.Datagram
{

    /// <summary>
    /// Describes a datagram header.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly ref struct KPacketHeader<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Writes the header to the buffer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="version"></param>
        /// <param name="sender"></param>
        /// <param name="magic"></param>
        /// <param name="type"></param>
        public static void Write(IBufferWriter<byte> writer, uint version, in TKNodeId sender, uint magic, KPacketType type)
        {
            // write Version
            BinaryPrimitives.WriteUInt32BigEndian(writer.GetMemory(sizeof(uint)).Span, version);
            writer.Advance(sizeof(uint));

            // writer Sender
            sender.Write(writer);

            // write Magic
            BinaryPrimitives.WriteUInt32BigEndian(writer.GetMemory(sizeof(uint)).Span, magic);
            writer.Advance(sizeof(uint));

            // write Type
            writer.GetSpan(sizeof(sbyte))[0] = (byte)(sbyte)type;
            writer.Advance(sizeof(sbyte));
        }

        readonly ReadOnlySpan<byte> span;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="span"></param>
        public KPacketHeader(ReadOnlySpan<byte> span)
        {
            this.span = span;
        }

        /// <summary>
        /// Gets the version of the datagram.
        /// </summary>
        public uint Version => BinaryPrimitives.ReadUInt32BigEndian(span.Slice(KPacketHeaderInfo<TKNodeId>.VersionOffset, KPacketHeaderInfo<TKNodeId>.VersionSize));

        /// <summary>
        /// Gets the sender of the datagram.
        /// </summary>
        public TKNodeId Sender => KNodeId<TKNodeId>.Read(span.Slice(KPacketHeaderInfo<TKNodeId>.SenderOffset, KPacketHeaderInfo<TKNodeId>.SenderSize));

        /// <summary>
        /// Gets the value identifying this datagram in a request/response lifecycle.
        /// </summary>
        public uint Magic => BinaryPrimitives.ReadUInt32BigEndian(span.Slice(KPacketHeaderInfo<TKNodeId>.MagicOffset, KPacketHeaderInfo<TKNodeId>.MagicSize));

        /// <summary>
        /// Gets the type of request.
        /// </summary>
        public KPacketType Type => (KPacketType)(sbyte)span.Slice(KPacketHeaderInfo<TKNodeId>.TypeOffset, KPacketHeaderInfo<TKNodeId>.TypeSize)[0];

    }

}
