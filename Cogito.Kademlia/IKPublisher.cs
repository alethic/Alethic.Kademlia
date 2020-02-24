namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a component that periodically publishes values owned by the node.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public interface IKPublisher<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {



    }

}
