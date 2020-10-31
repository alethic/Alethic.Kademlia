using MessagePack;

namespace Cogito.Kademlia.MessagePack.Structures
{

    [MessagePackObject]
    public class IpEndpoint
    {

        [Key(0)]
        public IpAddress Address { get; set; }

        [Key(1)]
        public uint Port { get; set; }

    }

}
