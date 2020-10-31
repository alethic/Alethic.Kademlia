using MessagePack;

namespace Cogito.Kademlia.MessagePack.Structures
{

    [MessagePackObject]
    public class FindNodeResponse : Response
    {

        [Key(0)]
        public Peer[] Peers { get; set; }

    }

}