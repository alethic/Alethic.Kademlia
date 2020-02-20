namespace Cogito.Kademlia.Network.Datagram
{

    /// <summary>
    /// Provides various utiltiies for understanding a datagram header.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    static class KPacketHeaderInfo<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        public static readonly int VersionOffset = 0;
        public static readonly int VersionSize = sizeof(uint);
        public static readonly int SenderOffset = VersionOffset + VersionSize;
        public static readonly int SenderSize = KNodeId<TKNodeId>.SizeOf();
        public static readonly int MagicOffset = SenderOffset + SenderSize;
        public static readonly int MagicSize = sizeof(uint);
        public static readonly int TypeOffset = MagicOffset + MagicSize;
        public static readonly int TypeSize = sizeof(sbyte);
        public static readonly int Size = TypeOffset + TypeSize;

    }

}
