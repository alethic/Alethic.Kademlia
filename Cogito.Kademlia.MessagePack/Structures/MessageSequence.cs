using MessagePack;

namespace Cogito.Kademlia.MessagePack.Structures
{

    [MessagePackObject]
    public class MessageSequence
    {

        [Key(0)]
        public ulong Network { get; set; }

        [Key(1)]
        public Message[] Messages { get; set; }

    }

}
