namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a response to a FIND_NODE request.
    /// </summary>
    public readonly struct KFindNodeResponse<TNodeId> : IKResponseBody<TNodeId>
        where TNodeId : unmanaged
    {

        readonly KPeerEndpointInfo<TNodeId>[] peers;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="peers"></param>
        public KFindNodeResponse(KPeerEndpointInfo<TNodeId>[] peers)
        {
            this.peers = peers;
        }

        /// <summary>
        /// Gets the set of peers and their endpoints returned by the lookup.
        /// </summary>
        public KPeerEndpointInfo<TNodeId>[] Peers => peers;

    }

}
