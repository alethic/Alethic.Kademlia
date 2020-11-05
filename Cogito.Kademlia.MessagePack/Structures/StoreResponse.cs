using MessagePack;

namespace Cogito.Kademlia.MessagePack.Structures
{

    [MessagePackObject]
    public class StoreResponse : ResponseBody
    {

        [Key(8)]
        public StoreResponseStatus Status { get; set; }

    }

}