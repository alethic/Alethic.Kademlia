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
        /// <param name="protcol"></param>
        /// <param name="buffer"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        void Encode(IKProtocol<TKNodeId> protcol, IBufferWriter<byte> buffer, IEnumerable<IKMessage<TKNodeId>> messages);

    }

}
