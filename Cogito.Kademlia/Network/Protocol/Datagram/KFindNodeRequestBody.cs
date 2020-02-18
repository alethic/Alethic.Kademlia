namespace Cogito.Kademlia.Network.Protocol.Datagram
{

    /// <summary>
    /// Describes the data to send for a FIND_NODE request.
    /// </summary>
    public readonly ref struct KFindNodeRequestBody<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly TKNodeId nodeId;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="magic"></param>
        /// <param name="nodeId"></param>
        public KFindNodeRequestBody(in TKNodeId nodeId)
        {
            this.nodeId = nodeId;
        }

        /// <summary>
        /// Gets or sets the node ID to be located.
        /// </summary>
        public TKNodeId NodeId => nodeId;

    }

}
