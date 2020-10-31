using MessagePack;

namespace Cogito.Kademlia.MessagePack.Structures
{

    [MessagePackObject]
    public class Ipv4Address : IpAddress
    {

        [Key(0)]
        public uint Value { get; set; }

    }

}
