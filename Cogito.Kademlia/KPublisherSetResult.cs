namespace Cogito.Kademlia
{

    /// <summary>
    /// Descsribes the results of a store set operation.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KPublisherSetResult<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly KPublisherSetResultStatus status;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="status"></param>
        public KPublisherSetResult(KPublisherSetResultStatus status)
        {
            this.status = status;
        }

        /// <summary>
        /// Gets the status of the store operation.
        /// </summary>
        public KPublisherSetResultStatus Status => status;

    }

}
