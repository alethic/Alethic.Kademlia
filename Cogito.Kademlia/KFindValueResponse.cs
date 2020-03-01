namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a response to a FIND_VALUE request.
    /// </summary>
    public readonly struct KFindValueResponse<TKNodeId> : IKResponseData<TKNodeId>, IKMessageBody<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly KPeerEndpointInfo<TKNodeId>[] peers;
        readonly KValueInfo? value;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="peers"></param>
        /// <param name="value"></param>
        public KFindValueResponse(KPeerEndpointInfo<TKNodeId>[] peers, KValueInfo? value)
        {
            this.peers = peers;
            this.value = value;
        }

        /// <summary>
        /// Gets the set of peers and their endpoints returned by the lookup.
        /// </summary>
        public KPeerEndpointInfo<TKNodeId>[] Peers => peers;

        /// <summary>
        /// Gets the value that was located.
        /// </summary>
        public KValueInfo? Value => value;

    }

}
