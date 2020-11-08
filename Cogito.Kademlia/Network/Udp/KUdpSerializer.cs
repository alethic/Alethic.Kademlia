using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cogito.Kademlia.Network.Udp
{

    /// <summary>
    /// Provides methods to read and write UDP datagrams.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public class KUdpSerializer<TNodeId> : IKUdpSerializer<TNodeId>
        where TNodeId : unmanaged
    {

        readonly IEnumerable<IKMessageFormat<TNodeId>> formats;
        readonly uint magic;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="magic"></param>
        /// <param name="formats"></param>
        public KUdpSerializer(IEnumerable<IKMessageFormat<TNodeId>> formats, uint magic)
        {
            this.formats = formats ?? throw new ArgumentNullException(nameof(formats));
            this.magic = magic;
        }

        /// <summary>
        /// Attempts to read a message sequence from the input data.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public KUdpPacket<TNodeId> Read(ReadOnlyMemory<byte> buffer, IKMessageContext<TNodeId> context)
        {
            if (BinaryPrimitives.ReadUInt32LittleEndian(buffer.Span) != magic)
                throw new KProtocolException(KProtocolError.Invalid, "Packet missing magic.");

            // advance past magic number
            buffer = buffer.Slice(sizeof(uint));

            // format ends at first NUL
            var formatEnd = buffer.Span.IndexOf((byte)0x00);
            if (formatEnd < 0)
                throw new KProtocolException(KProtocolError.Invalid, "Malformed packet.");

            // extract encoded format type
#if NETSTANDARD2_0
            var contentType = Encoding.UTF8.GetString(buffer.Span.Slice(0, formatEnd).ToArray());
#else
            var contentType = Encoding.UTF8.GetString(buffer.Span.Slice(0, formatEnd));
#endif
            if (contentType == null)
                throw new KProtocolException(KProtocolError.Invalid, "Packet missing content type.");

            var format = formats.FirstOrDefault(i => i.ContentType == contentType);
            if (format == null)
                throw new KProtocolException(KProtocolError.Invalid, $"Unknown format: '{contentType}'.");

            // advance past format
            buffer = buffer.Slice(formatEnd + 1);

            // decode message sequence
            return new KUdpPacket<TNodeId>(format.ContentType, format.Decode(context, new ReadOnlySequence<byte>(buffer)));
        }

        /// <summary>
        /// Writes a message sequence to the writer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="context"></param>
        /// <param name="format"></param>
        /// <param name="messages"></param>
        public void Write(IBufferWriter<byte> writer, IKMessageContext<TNodeId> context, in KMessageSequence<TNodeId> messages)
        {
            var format = formats.FirstOrDefault(i => context.Formats.Contains(i.ContentType));
            if (format == null)
                throw new KProtocolException(KProtocolError.Invalid, "Could not negotiate allowable format type.");

            // write protocol magic
            BinaryPrimitives.WriteUInt32LittleEndian(writer.GetSpan(sizeof(uint)), magic);
            writer.Advance(sizeof(uint));

            // write message format
            writer.Write(Encoding.UTF8.GetBytes(format.ContentType));
            writer.Write(new byte[] { 0x00 });

            // write message sequence
            format.Encode(context, writer, messages);
        }

    }

}
