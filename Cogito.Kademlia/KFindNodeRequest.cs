namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a FIND_NODE request.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KFindNodeRequest<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly TKNodeId key;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        public KFindNodeRequest(in TKNodeId key)
        {
            this.key = key;
        }

        /// <summary>
        /// Specifies the node ID to be located.
        /// </summary>
        public TKNodeId Key => key;

    }

}
