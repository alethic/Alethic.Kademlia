namespace Cogito.Kademlia.Network
{

    /// <summary>
    /// Defines a <see cref="IKProtocol{TKNodeId}"/> type that communicates over IP networks.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public interface IKIpProtocol<TKNodeId> : IKProtocol<TKNodeId>, IKIpProtocolResourceProvider<TKNodeId>
        where TKNodeId : unmanaged
    {



    }

}
