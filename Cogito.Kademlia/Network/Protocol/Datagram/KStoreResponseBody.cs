namespace Cogito.Kademlia.Network.Protocol.Datagram
{

    /// <summary>
    /// Describes the data to send for a STORE response.
    /// </summary>
    public readonly ref struct KStoreResponseBody<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly TKNodeId key;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        public KStoreResponseBody(TKNodeId key)
        {
            this.key = key;
        }

        /// <summary>
        /// Gets the key to store.
        /// </summary>
        public TKNodeId Key => key;

    }

}
