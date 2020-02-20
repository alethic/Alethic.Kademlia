namespace Cogito.Kademlia.Network.Datagram
{

    /// <summary>
    /// Provides various utilties for understanding a datagram header.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    static class KPacketPingRequestInfo<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        public static readonly int EndpointsOffset = 0;

    }

}
