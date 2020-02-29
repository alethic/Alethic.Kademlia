namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a response to a STORE request.
    /// </summary>
    public readonly struct KStoreResponse<TKNodeId> : IKResponseData<TKNodeId>, IKMessageBody<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly TKNodeId key;
        readonly KStoreResponseStatus status;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        public KStoreResponse(in TKNodeId key, KStoreResponseStatus status)
        {
            this.key = key;
            this.status = status;
        }

        /// <summary>
        /// Gets the key to store.
        /// </summary>
        public TKNodeId Key => key;

        /// <summary>
        /// Gets the result of the STORE operation.
        /// </summary>
        public KStoreResponseStatus Status => status;

    }

}
