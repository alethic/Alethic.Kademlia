using Cogito.Kademlia.Core;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a list of endpoints.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public interface IKEndpointSet<TKNodeId> : IOrderedSet<IKEndpoint<TKNodeId>>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {



    }

}
