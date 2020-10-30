namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a standard featureful peer data implementation.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public class KNodeData<TKNodeId> : IKEndpointProvider<TKNodeId>
        where TKNodeId : unmanaged
    {

        readonly KEndpointSet<TKNodeId> endpoints;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public KNodeData()
        {
            endpoints = new KEndpointSet<TKNodeId>();
        }

        /// <summary>
        /// Gets the endpoints associated with the peer.
        /// </summary>
        public IKEndpointSet<TKNodeId> Endpoints => endpoints;

    }

}
