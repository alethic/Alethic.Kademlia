namespace Cogito.Kademlia.Network.Datagram
{

    /// <summary>
    /// Provides a description of the layout.
    /// </summary>
    static class KPacketPeerInfo<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        public static readonly int IdOffset = 0;
        public static readonly int IdSize = KNodeId<TKNodeId>.SizeOf();
        public static readonly int EndpointsOffset = IdOffset + IdSize;

    }

}
