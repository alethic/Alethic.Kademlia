using System.Buffers;

namespace Alethic.Kademlia.MessagePack
{

    public class KMessagePackMessageFormat<TNodeId> : IKMessageFormat<TNodeId>
        where TNodeId : unmanaged
    {

        static readonly KMessagePackMessageEncoder<TNodeId> encoder = new KMessagePackMessageEncoder<TNodeId>();
        static readonly KMessagePackMessageDecoder<TNodeId> decoder = new KMessagePackMessageDecoder<TNodeId>();

        public string ContentType => "application/msgpack";

        public KMessageSequence<TNodeId> Decode(IKMessageContext<TNodeId> context, ReadOnlySequence<byte> buffer)
        {
            return decoder.Decode(context, buffer);
        }

        public void Encode(IKMessageContext<TNodeId> context, IBufferWriter<byte> buffer, KMessageSequence<TNodeId> messages)
        {
            encoder.Encode(context, buffer, messages);
        }
    }

}
