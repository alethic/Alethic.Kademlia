namespace Cogito.Kademlia
{

    /// <summary>
    /// Provides <see cref="IKEndpoint{TKNodeId}"/> instances.
    /// </summary>
    public interface IKEndpointProvider<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Gets the set of endpoints.
        /// </summary>
        IKEndpointSet<TKNodeId> Endpoints { get; }

    }

}
