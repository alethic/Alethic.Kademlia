using MessagePack;

namespace Cogito.Kademlia.MessagePack.Structures
{

    [MessagePackObject]
    public class FindValueResponse : ResponseBody
    {

        [Key(8)]
        public Peer[] Peers { get; set; }

        [Key(9)]
        public bool HasValue { get; set; }

        [Key(10)]
        public ValueInfo Value { get; set; }

    }

}