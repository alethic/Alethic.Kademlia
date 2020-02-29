namespace Cogito.Kademlia
{

    /// <summary>
    /// Descsribes the results of a store set operation.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KPublisherSetResult<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly TKNodeId key;
        readonly KPublisherSetResultStatus status;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="status"></param>
        public KPublisherSetResult(in TKNodeId key, KPublisherSetResultStatus status)
        {
            this.key = key;
            this.status = status;
        }

        /// <summary>
        /// Gets the key that was set.
        /// </summary>
        public TKNodeId Key => key;

        /// <summary>
        /// Gets the status of the store operation.
        /// </summary>
        public KPublisherSetResultStatus Status => status;
    }

}
