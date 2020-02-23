using System.Buffers;

namespace Cogito.Kademlia.Protocols
{

    /// <summary>
    /// Defines an instance capable of decoding messages to a buffer.
    /// </summary>
    public interface IKMessageEncoder<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Encodes the sequence of messages into the buffer.
        /// </summary>
        /// <param name="protocol"></param>
        /// <param name="messages"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        void Encode(IKProtocol<TKNodeId> protocol, IBufferWriter<byte> buffer, KMessageSequence<TKNodeId> messages);

    }

}
