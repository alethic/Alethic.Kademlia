namespace Cogito.Kademlia
{

    /// <summary>
    /// Represents a Kademlia network engine.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    /// <typeparam name="TKPeerData"></typeparam>
    public interface IKEngine<TKNodeId, TKPeerData>
        where TKNodeId : struct, IKNodeId<TKNodeId>
    {



    }

}