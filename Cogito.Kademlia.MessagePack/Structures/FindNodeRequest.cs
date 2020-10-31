using MessagePack;

namespace Cogito.Kademlia.MessagePack.Structures
{

    [MessagePackObject]
    public class FindNodeRequest : Request
    {

        [Key(0)]
        public byte[] Key { get; set; } 

    }

}