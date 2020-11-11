namespace Alethic.Kademlia
{

    /// <summary>
    /// Describes a response to a FIND_NODE request.
    /// </summary>
    public readonly struct KFindNodeResponse<TNodeId> : IKResponseBody<TNodeId>
        where TNodeId : unmanaged
    {

        readonly KNodeInfo<TNodeId>[] nodes;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="nodes"></param>
        public KFindNodeResponse(KNodeInfo<TNodeId>[] nodes)
        {
            this.nodes = nodes;
        }

        /// <summary>
        /// Gets the set of nodes and their endpoints returned by the lookup.
        /// </summary>
        public KNodeInfo<TNodeId>[] Nodes => nodes;

    }

}
