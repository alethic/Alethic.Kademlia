
using MessagePack;

namespace Cogito.Kademlia.MessagePack.Structures
{

    [MessagePackObject]
    public class Message
    {

        [Key(0)]
        public Header Header { get; set; }

        [Key(1)]
        public Body Body { get; set; } 

    }

}
