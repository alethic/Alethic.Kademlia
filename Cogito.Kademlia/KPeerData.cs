namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a standard featureful peer data implementation.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public class KPeerData<TKNodeId> : IKEndpointProvider<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly KEndpointList<TKNodeId> endpoints;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public KPeerData()
        {
            endpoints = new KEndpointList<TKNodeId>();
        }

        /// <summary>
        /// Gets the endpoints associated with the peer.
        /// </summary>
        public IKEndpointList<TKNodeId> Endpoints => endpoints;

    }

}
