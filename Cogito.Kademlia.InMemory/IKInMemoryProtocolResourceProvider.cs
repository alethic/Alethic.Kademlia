namespace Cogito.Kademlia.InMemory
{

    /// <summary>
    /// Provides <see cref="IKEndpoint{TKNodeId}"/> instances from an in memory node ID.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public interface IKInMemoryProtocolResourceProvider<TKNodeId> : IKProtocolResourceProvider<TKNodeId>
        where TKNodeId : unmanaged
    {

        /// <summary>
        /// Obtains a <see cref="IKEndpoint{TKNodeId}"/> from an in memory node ID.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        IKEndpoint<TKNodeId> CreateEndpoint(in TKNodeId node);

    }

}
