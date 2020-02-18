namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a response to a STORE request.
    /// </summary>
    public readonly struct KStoreResponse<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly TKNodeId key;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        public KStoreResponse(TKNodeId key)
        {
            this.key = key;
        }

        /// <summary>
        /// Gets the key to store.
        /// </summary>
        public TKNodeId Key => key;

    }

}
