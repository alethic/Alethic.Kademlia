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

        readonly Span<byte> span;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="span"></param>
        public KPacketHeader(Span<byte> span)
        {
            this.span = span;

            // currently only supported version
            if (Version != 1)
                throw new InvalidOperationException();
        }

        /// <summary>
        /// Gets the version of the data gram.
        /// </summary>
        public uint Version
        {
            get => new KPacketHeaderReadOnly<TKNodeId>(span).Version;
            set => BinaryPrimitives.WriteUInt32BigEndian(span.Slice(KPacketHeaderInfo<TKNodeId>.VersionOffset, KPacketHeaderInfo<TKNodeId>.VersionSize), value);
        }

        /// <summary>
        /// Gets the sender of the datagram.
        /// </summary>
        public TKNodeId Sender
        {
            get => new KPacketHeaderReadOnly<TKNodeId>(span).Sender;
            set => value.Write(span.Slice(KPacketHeaderInfo<TKNodeId>.SenderOffset, KPacketHeaderInfo<TKNodeId>.SenderSize));
        }

        /// <summary>
        /// Gets the value identifying this datagram in a request/response lifecycle.
        /// </summary>
        public uint Magic
        {
            get => new KPacketHeaderReadOnly<TKNodeId>(span).Magic;
            set => BinaryPrimitives.WriteUInt32BigEndian(span.Slice(KPacketHeaderInfo<TKNodeId>.MagicOffset, KPacketHeaderInfo<TKNodeId>.MagicSize), value);
        }

        /// <summary>
        /// Gets the type of request.
        /// </summary>
        public KPacketType Type
        {
            get => new KPacketHeaderReadOnly<TKNodeId>(span).Type;
            set => span.Slice(KPacketHeaderInfo<TKNodeId>.TypeOffset, KPacketHeaderInfo<TKNodeId>.TypeSize)[0] = (byte)(sbyte)value;
        }


    }

}
