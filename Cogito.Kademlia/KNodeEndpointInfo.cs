namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a peer and its associated endpoints.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public struct KNodeEndpointInfo<TNodeId>
        where TNodeId : unmanaged
    {

        readonly TNodeId id;
        readonly KProtocolEndpointSet<TNodeId> endpoints;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="endpoints"></param>
        public KNodeEndpointInfo(in TNodeId id, KProtocolEndpointSet<TNodeId> endpoints)
        {
            this.id = id;
            this.endpoints = endpoints;
        }

        /// <summary>
        /// Gets the node ID of the peer.
        /// </summary>
        public TNodeId Id => id;

        /// <summary>
        /// Gets the set of known endpoints of the peer.
        /// </summary>
        public KProtocolEndpointSet<TNodeId> Endpoints => endpoints;

    }

}
