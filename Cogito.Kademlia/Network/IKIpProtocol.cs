namespace Cogito.Kademlia.Network
{

    /// <summary>
    /// Defines a <see cref="IKProtocol{TKNodeId}"/> type that communicates over IP networks.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public interface IKIpProtocol<TKNodeId> : IKProtocol<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Creates a new <see cref="KIpProtocolEndpoint<TKNodeId>"/> for the provider.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        KIpProtocolEndpoint<TKNodeId> CreateEndpoint(in KIpEndpoint endpoint);

    }

}
