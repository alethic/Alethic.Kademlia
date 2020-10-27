namespace Cogito.Kademlia
{

    /// <summary>
    /// Represents an item within a bucket.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    /// <typeparam name="TKPeerData"></typeparam>
    public readonly struct KBucketItem<TKNodeId, TKPeerData>
        where TKNodeId : unmanaged
    {

        readonly TKNodeId id;
        readonly TKPeerData data;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="endpoints"></param>
        public KBucketItem(in TKNodeId id, in TKPeerData data)
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
        public TKPeerData Data => data;

    }

}
