namespace Cogito.Kademlia.Network.Datagram
{

    /// <summary>
    /// Provides various utiltiies for understanding a datagram header.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    static class KPacketStoreRequestInfo<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        public static readonly int KeySize = KNodeId<TKNodeId>.SizeOf();
        public static readonly int KeyOffset = 0;
        public static readonly int ValueCountSize = sizeof(uint);
        public static readonly int ValueCountOffset = KeyOffset + KeySize;
        public static readonly int ValueOffset = ValueCountOffset + ValueCountSize;

    }

}
