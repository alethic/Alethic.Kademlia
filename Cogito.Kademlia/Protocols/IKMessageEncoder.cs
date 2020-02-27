using System.Buffers;

namespace Cogito.Kademlia.Protocols
{

    /// <summary>
    /// Defines an instance capable of decoding messages to a buffer.
    /// </summary>
    public interface IKMessageEncoder<TKNodeId> : IKMessageEncoder<TKNodeId, IKProtocolResourceProvider<TKNodeId>>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {



    }

    /// <summary>
    /// Defines an instance capable of decoding messages to a buffer.
    /// </summary>
    public interface IKMessageEncoder<TKNodeId, in TKProtocolResourceProvider>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
        where TKProtocolResourceProvider : IKProtocolResourceProvider<TKNodeId>
    {

        /// <summary>
        /// Encodes the sequence of messages into the buffer.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="messages"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        void Encode(TKProtocolResourceProvider resources, IBufferWriter<byte> buffer, KMessageSequence<TKNodeId> messages);

    }

}
