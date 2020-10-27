namespace Cogito.Kademlia.Protocols
{

    /// <summary>
    /// Describes a datagram header.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KMessageHeader<TKNodeId>
        where TKNodeId : unmanaged
    {

        readonly TKNodeId sender;
        readonly ulong magic;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="magic"></param>
        public KMessageHeader(in TKNodeId sender, ulong magic)
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
        public ulong Magic => magic;

    }

}
