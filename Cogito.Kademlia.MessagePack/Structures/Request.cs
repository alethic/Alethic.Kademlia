using MessagePack;

namespace Cogito.Kademlia.MessagePack.Structures
{

    [MessagePackObject]
    public class Request : Message
    {

        public RequestBody Body { get; set; }

    }

}
