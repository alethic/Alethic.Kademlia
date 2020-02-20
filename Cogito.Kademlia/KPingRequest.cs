namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a PING request.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KPingRequest<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly IKEndpoint<TKNodeId>[] endpoints;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="endpoints"></param>
        public KPingRequest(IKEndpoint<TKNodeId>[] endpoints)
        {
            this.endpoints = endpoints;
        }

        public IKEndpoint<TKNodeId>[] Endpoints => endpoints;

    }

}
