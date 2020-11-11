using MessagePack;

namespace Alethic.Kademlia.MessagePack.Structures
{

    [MessagePackObject]
    public struct Header
    {

        [Key(0)]
        public byte[] Sender { get; set; }

        [Key(1)]
        public uint ReplyId { get; set; }

    }

}
