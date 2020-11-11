using MessagePack;

namespace Alethic.Kademlia.MessagePack.Structures
{

    [MessagePackObject]
    public class Response : Message
    {

        [Key(4)]
        public ResponseStatus Status { get; set; }

        [Key(5)]
        public ResponseBody Body { get; set; }

    }

}
