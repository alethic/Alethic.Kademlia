namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a response to a FIND_NODE request.
    /// </summary>
    public readonly struct KFindNodeResponse<TKNodeId> : IKResponseData<TKNodeId>, IKMessageBody<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly KPeerEndpointInfo<TKNodeId>[] peers;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="peers"></param>
        public KFindNodeResponse(KPeerEndpointInfo<TKNodeId>[] peers)
        {
            this.peers = peers;
        }

        /// <summary>
        /// Gets the set of peers and their endpoints returned by the lookup.
        /// </summary>
        public KPeerEndpointInfo<TKNodeId>[] Peers => peers;

    }

}
