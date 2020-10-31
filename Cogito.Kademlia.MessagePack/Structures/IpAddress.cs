namespace Cogito.Kademlia.MessagePack.Structures
{

    [global::MessagePack.Union(0, typeof(Ipv4Address))]
    [global::MessagePack.Union(1, typeof(Ipv6Address))]
    public abstract class IpAddress
    {



    }

}
