namespace Alethic.Kademlia
{

    /// <summary>
    /// Represents an item within a bucket.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public readonly struct KBucketItem<TNodeId>
        where TNodeId : unmanaged
    {

        readonly TNodeId nodeId;
        readonly KProtocolEndpointSet<TNodeId> endpoints;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="endpoints"></param>
        public KBucketItem(in TNodeId id)
        {
            this.nodeId = id;
            this.endpoints = new KProtocolEndpointSet<TNodeId>();
        }

        /// <summary>
        /// Gets the node ID of the peer.
        /// </summary>
        public TNodeId NodeId => nodeId;

        /// <summary>
        /// Gets the endpoints associated with the node.
        /// </summary>
        public KProtocolEndpointSet<TNodeId> Endpoints => endpoints;

    }

}
