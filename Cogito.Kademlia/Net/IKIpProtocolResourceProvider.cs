namespace Cogito.Kademlia.Net
{

    /// <summary>
    /// Provides <see cref="IKEndpoint{TKNodeId}"/> instances from IP primitives.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public interface IKIpProtocolResourceProvider<TKNodeId> : IKProtocolResourceProvider<TKNodeId>
        where TKNodeId : unmanaged
    {

        /// <summary>
        /// Obtains a <see cref="IKEndpoint{TKNodeId}"/> from an IP address.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        IKEndpoint<TKNodeId> CreateEndpoint(in KIpEndpoint endpoint);

    }

}
