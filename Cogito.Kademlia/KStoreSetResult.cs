namespace Cogito.Kademlia
{

    /// <summary>
    /// Descsribes the results of a store set operation.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KStoreSetResult<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly KStoreSetResultStatus status;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="status"></param>
        public KStoreSetResult(KStoreSetResultStatus status)
        {
            this.status = status;
        }

        /// <summary>
        /// Gets the status of the store operation.
        /// </summary>
        public KStoreSetResultStatus Status => status;

    }

}
