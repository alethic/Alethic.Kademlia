using MessagePack;

namespace Cogito.Kademlia.MessagePack.Structures
{

    [MessagePackObject]
    public class FindValueResponse : Response
    {

        [Key(0)]
        public Peer[] Peers { get; set; }

        [Key(1)]
        public bool HasValue { get; set; }

        [Key(2)]
        public ValueInfo Value { get; set; }

    }

}