using MessagePack;

namespace Cogito.Kademlia.MessagePack.Structures
{

    [MessagePackObject]
    public class Header
    {

        [Key(0)]
        public byte[] Sender { get; set; }

        [Key(1)]
        public ulong Magic { get; set; }

    }

}
