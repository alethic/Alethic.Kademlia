using MessagePack;

namespace Alethic.Kademlia.MessagePack.Structures
{

    [MessagePackObject]
    public class StoreResponse : ResponseBody
    {

        [Key(8)]
        public StoreResponseStatus Status { get; set; }

    }

}