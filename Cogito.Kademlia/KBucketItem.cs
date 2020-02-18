namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes an item within a <see cref="KBucket{TKNodeId, TKPeerData}".
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    /// <typeparam name="TKPeerData"></typeparam>
    readonly struct KBucketItem<TKNodeId, TKPeerData>
        where TKNodeId : IKNodeId<TKNodeId>
    {

        readonly TKNodeId id;
        readonly TKPeerData data;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="data"></param>
        /// <param name="events"></param>
        public KBucketItem(TKNodeId id, TKPeerData data)
        {
            this.id = id;
            this.data = data;
        }

        /// <summary>
        /// Gets the node ID maintained by this bucket item.
        /// </summary>
        public TKNodeId Id => id;

        /// <summary>
        /// Gets the node data maintained by this bucket item.
        /// </summary>
        public TKPeerData Data => data;

    }

}
