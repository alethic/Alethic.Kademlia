using System.Buffers;

namespace Alethic.Kademlia
{

    /// <summary>
    /// Defines an instance capable of encoding and decoding messages out of a buffer.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public interface IKMessageFormat<TNodeId>
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Gets the MIME type the decoder is capable of decoding.
        /// </summary>
        string ContentType { get; }

        /// <summary>
        /// Encodes the sequence of messages into the buffer.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="messages"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        void Encode(IKMessageContext<TNodeId> context, IBufferWriter<byte> buffer, KMessageSequence<TNodeId> messages);

        /// <summary>
        /// Decodes the sequence of messages from the buffer.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        KMessageSequence<TNodeId> Decode(IKMessageContext<TNodeId> context, ReadOnlySequence<byte> buffer);

    }

}
