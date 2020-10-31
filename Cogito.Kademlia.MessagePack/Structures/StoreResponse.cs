using MessagePack;

namespace Cogito.Kademlia.MessagePack.Structures
{

    [MessagePackObject]
    public class StoreResponse : Request
    {

        [Key(0)]
        public StoreResponseStatus Status { get; set; }

    }

}