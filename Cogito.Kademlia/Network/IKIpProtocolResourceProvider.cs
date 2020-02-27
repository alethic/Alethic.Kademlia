namespace Cogito.Kademlia.Network
{

    /// <summary>
    /// Provides <see cref="IKEndpoint{TKNodeId}"/> instances from IP primitives.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public interface IKIpProtocolResourceProvider<TKNodeId> : IKProtocolResourceProvider<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Obtains a <see cref="IKEndpoint{TKNodeId}"/> from an IP address.
        /// </summary>
        /// <param name="ep"></param>
        /// <returns></returns>
        KIpProtocolEndpoint<TKNodeId> CreateEndpoint(in KIpEndpoint ep);

    }

}
