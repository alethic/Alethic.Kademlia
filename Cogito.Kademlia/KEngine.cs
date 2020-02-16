namespace Cogito.Kademlia
{

    /// <summary>
    /// Provides an implementation of a Kademlia network engine.
    /// </summary>
    public class KEngine<TKNodeId, TKPeerData> : IKEngine<TKNodeId, TKPeerData>
        where TKNodeId : struct, IKNodeId<TKNodeId>
    {

        readonly TKNodeId self;
        readonly IKProtocol<TKNodeId, TKPeerData> protocol;
        readonly IKRoutingTable<TKNodeId, TKPeerData> routes;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="protocol"></param>
        /// <param name="routes"></param>
        public KEngine(TKNodeId self, IKProtocol<TKNodeId, TKPeerData> protocol, IKRoutingTable<TKNodeId, TKPeerData> routes)
        {

        }

    }

}
