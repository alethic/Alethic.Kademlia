namespace Cogito.Kademlia.Protocols
{

    /// <summary>
    /// Describes a datagram header.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KMessageHeader<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly TKNodeId sender;
        readonly uint magic;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="version"></param>
        /// <param name="sender"></param>
        /// <param name="magic"></param>
        public KMessageHeader(in TKNodeId sender, uint magic)
        {
            this.sender = sender;
            this.magic = magic;
        }

        /// <summary>
        /// Gets the sender of the datagram.
        /// </summary>
        public TKNodeId Sender => sender;

        /// <summary>
        /// Gets the value identifying this datagram in a request/response lifecycle.
        /// </summary>
        public uint Magic => magic;

    }

}
