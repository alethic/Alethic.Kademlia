namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes an item within a <see cref="KBucket{TKNodeId, TKNodeData}".
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    /// <typeparam name="TKNodeData"></typeparam>
    readonly struct KBucketItem<TKNodeId, TKNodeData>
        where TKNodeId : struct, IKNodeId<TKNodeId>
    {

        readonly TKNodeId id;
        readonly TKNodeData data;
        readonly IKNodeEvents events;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="data"></param>
        /// <param name="events"></param>
        public KBucketItem(TKNodeId id, TKNodeData data, IKNodeEvents events)
        {
            this.id = id;
            this.data = data;
            this.events = events;
        }

        /// <summary>
        /// Gets the node ID maintained by this bucket item.
        /// </summary>
        public TKNodeId Id => id;

        /// <summary>
        /// Gets the node data maintained by this bucket item.
        /// </summary>
        public TKNodeData Data => data;

        /// <summary>
        /// Gets the node event sink associated with the bucket item.
        /// </summary>
        public IKNodeEvents Events => events;

    }

}
