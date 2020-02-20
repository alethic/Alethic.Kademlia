namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a response to a PING request.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KPingResponse<TKNodeId> : IKResponseData<TKNodeId>, IKMessageBody<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly IKEndpoint<TKNodeId>[] endpoints;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="endpoints"></param>
        public KPingResponse(IKEndpoint<TKNodeId>[] endpoints)
        {
            this.endpoints = endpoints;
        }

        /// <summary>
        /// Gets the set of endpoints to return to the ping requester.
        /// </summary>
        public IKEndpoint<TKNodeId>[] Endpoints => endpoints;

    }

}
