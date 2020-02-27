using System.Buffers;

namespace Cogito.Kademlia.Protocols
{

    /// <summary>
    /// Defines an instance capable of decoding messages out of a buffer.
    /// </summary>
    public interface IKMessageDecoder<TKNodeId> : IKMessageDecoder<TKNodeId, IKProtocolResourceProvider<TKNodeId>>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {



    }

    /// <summary>
    /// Defines an instance capable of decoding messages out of a buffer.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public interface IKMessageDecoder<TKNodeId, in TKProtocolResourceProvider>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
        where TKProtocolResourceProvider : IKProtocolResourceProvider<TKNodeId>
    {

        /// <summary>
        /// Decodes the sequence of messages from the buffer.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        KMessageSequence<TKNodeId> Decode(TKProtocolResourceProvider resources, ReadOnlySequence<byte> buffer);

    }

}
