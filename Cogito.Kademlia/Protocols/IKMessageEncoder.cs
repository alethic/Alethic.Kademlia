using System.Buffers;
using System.Collections.Generic;

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
        /// <param name="buffer"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        void Encode(IBufferWriter<byte> buffer, IEnumerable<IKMessage<TKNodeId>> messages);

        /// <summary>
        /// Encodes a single message into the buffer.
        /// </summary>
        /// <param name="buyffer"></param>
        /// <param name="message"></param>
        void Encode(IBufferWriter<byte> buyffer, IKMessage<TKNodeId> message);

    }

}
