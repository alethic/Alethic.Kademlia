using MessagePack;

namespace Alethic.Kademlia.MessagePack.Structures
{

    [MessagePackObject]
    public class Request : Message
    {

        [Key(5)]
        public RequestBody Body { get; set; }

    }

}
