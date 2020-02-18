namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a response to a FIND_NODE request.
    /// </summary>
    public readonly struct KFindNodeResponse<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly TKNodeId nodeId;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="magic"></param>
        /// <param name="nodeId"></param>
        public KFindNodeResponse(in TKNodeId nodeId)
        {
            this.nodeId = nodeId;
        }

        /// <summary>
        /// Gets or sets the node ID to be located.
        /// </summary>
        public TKNodeId NodeId => nodeId;

    }

}
