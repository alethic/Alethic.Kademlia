namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes an item within a <see cref="KBucket{TKNodeId, TKPeerData}".
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    /// <typeparam name="TKPeerData"></typeparam>
    readonly struct KBucketItem<TKNodeId, TKPeerData>
        where TKNodeId : struct, IKNodeId<TKNodeId>
    {

        readonly TKNodeId id;
        readonly TKPeerData data;
        readonly IKPeerEvents events;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="data"></param>
        /// <param name="events"></param>
        public KBucketItem(TKNodeId id, TKPeerData data, IKPeerEvents events)
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
        public TKPeerData Data => data;

        /// <summary>
        /// Gets the node event sink associated with the bucket item.
        /// </summary>
        public IKPeerEvents Events => events;

    }

}
