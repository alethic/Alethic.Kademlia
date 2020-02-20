namespace Cogito.Kademlia.Network.Datagram
{

    /// <summary>
    /// Provides various utilties for understanding a datagram header.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    static class KPacketFindValueResponseInfo<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        public static readonly int KeySize = KNodeId<TKNodeId>.SizeOf();
        public static readonly int KeyOffset = 0;
        public static readonly int ValueSizeOffset = KeyOffset + KeySize;
        public static readonly int ValueSizeSize = ValueSizeOffset + sizeof(uint);
        public static readonly int ValueOffset = ValueSizeOffset + ValueSizeSize;

    }

}
