using System.Buffers;

namespace Alethic.Kademlia.Protobuf
{

    public class KProtobufMessageFormat<TNodeId> : IKMessageFormat<TNodeId>
        where TNodeId : unmanaged
    {

        static readonly KProtobufMessageEncoder<TNodeId> encoder = new KProtobufMessageEncoder<TNodeId>();
        static readonly KProtobufMessageDecoder<TNodeId> decoder = new KProtobufMessageDecoder<TNodeId>();

        public string ContentType => "application/protobuf";

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
