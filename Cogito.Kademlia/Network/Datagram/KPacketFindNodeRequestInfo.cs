namespace Cogito.Kademlia.Network.Datagram
{

    /// <summary>
    /// Provides various utilties for understanding a datagram header.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    static class KPacketFindNodeRequestInfo<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        public static readonly int KeySize = KNodeId<TKNodeId>.SizeOf();
        public static readonly int KeyOffset = 0;

    }

}
