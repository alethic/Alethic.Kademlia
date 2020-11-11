using MessagePack;

namespace Alethic.Kademlia.MessagePack.Structures
{

    [MessagePackObject]
    public class FindNodeResponse : ResponseBody
    {

        [Key(8)]
        public Node[] Peers { get; set; }

    }

}