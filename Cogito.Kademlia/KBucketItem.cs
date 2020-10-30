namespace Cogito.Kademlia
{

    /// <summary>
    /// Represents an item within a bucket.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    /// <typeparam name="TKNodeData"></typeparam>
    public readonly struct KBucketItem<TKNodeId, TKNodeData>
        where TKNodeId : unmanaged
    {

        readonly TKNodeId id;
        readonly TKNodeData data;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="endpoints"></param>
        public KBucketItem(in TKNodeId id, in TKNodeData data)
        {
            this.id = id;
            this.data = data;
        }

        /// <summary>
        /// Gets the node ID of the peer.
        /// </summary>
        public TKNodeId Id => id;

        /// <summary>
        /// Gets the set of known endpoints of the peer.
        /// </summary>
        public TKNodeData Data => data;

    }

}
