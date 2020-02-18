namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a FIND_NODE request.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KFindNodeRequest<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly TKNodeId nodeId;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="nodeId"></param>
        public KFindNodeRequest(in TKNodeId nodeId)
        {
            this.nodeId = nodeId;
        }

        /// <summary>
        /// Specifies the node ID to be located.
        /// </summary>
        public TKNodeId NodeId => nodeId;

    }

}
