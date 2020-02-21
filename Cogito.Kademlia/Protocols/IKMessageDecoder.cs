using System.Buffers;
using System.Collections.Generic;

namespace Cogito.Kademlia.Protocols
{

    /// <summary>
    /// Defines an instance capable of decoding messages out of a buffer.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public interface IKMessageDecoder<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Decodes the sequence of messages from the buffer.
        /// </summary>
        /// <param name="protocol"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        IEnumerable<IKMessage<TKNodeId>> Decode(IKProtocol<TKNodeId> protocol, ReadOnlySequence<byte> buffer);

    }

}
