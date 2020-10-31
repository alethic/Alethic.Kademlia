using MessagePack;

namespace Cogito.Kademlia.MessagePack.Structures
{

    [MessagePackObject]
    public class FindValueRequest : Request
    {

        [Key(0)]
        public byte[] Key { get; set; }

    }

}