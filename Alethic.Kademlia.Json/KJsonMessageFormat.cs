using System.Buffers;

namespace Alethic.Kademlia.Json
{

    public class KJsonMessageFormat<TNodeId> : IKMessageFormat<TNodeId>
        where TNodeId : unmanaged
    {

        static readonly KJsonMessageEncoder<TNodeId> encoder = new KJsonMessageEncoder<TNodeId>();
        static readonly KJsonMessageDecoder<TNodeId> decoder = new KJsonMessageDecoder<TNodeId>();

        public string ContentType => "application/json";

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
