namespace Cogito.Kademlia.Network.Protocol.Datagram
{

    /// <summary>
    /// Describes a datagram header.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly ref struct KPacketHeader<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly TKNodeId sender;
        readonly TKNodeId target;
        readonly uint magic;
        readonly KPacketType type;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="target"></param>
        /// <param name="magic"></param>
        public KPacketHeader(in TKNodeId sender, in TKNodeId target, uint magic, KPacketType type)
        {
            this.sender = sender;
            this.target = target;
            this.magic = magic;
            this.type = type;
        }

        /// <summary>
        /// Gets the sender of the datagram.
        /// </summary>
        public TKNodeId Sender => sender;

        /// <summary>
        /// Gets the target of the datagram.
        /// </summary>
        public TKNodeId Target => target;

        /// <summary>
        /// Gets the value identifying this datagram in a request/response lifecycle.
        /// </summary>
        public uint Magic => magic;

        /// <summary>
        /// Gets the type of request.
        /// </summary>
        public KPacketType Type => type;

    }

}
