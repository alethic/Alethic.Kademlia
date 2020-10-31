using MessagePack;

namespace Cogito.Kademlia.MessagePack.Structures
{

    [MessagePackObject]
    public class Ipv6Address : IpAddress
    {

        [Key(0)]
        public byte[] Value { get; set; }

    }

}