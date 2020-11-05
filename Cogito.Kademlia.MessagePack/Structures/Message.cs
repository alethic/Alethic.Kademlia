
using MessagePack;

namespace Cogito.Kademlia.MessagePack.Structures
{

    [global::MessagePack.Union(0, typeof(Request))]
    [global::MessagePack.Union(1, typeof(Response))]
    public abstract class Message
    {

        [Key(0)]
        public Header Header { get; set; }

    }

}
