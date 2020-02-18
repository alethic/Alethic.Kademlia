using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a Kademlia routing table. A routing table is used to track the known nodes within the network and
    /// provide lookups.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    /// <typeparam name="TKPeerData"></typeparam>
    public interface IKRouter<TKNodeId, TKPeerData>
        where TKNodeId : IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Gets the ID of the node itself.
        /// </summary>
        TKNodeId SelfId { get; }

        /// <summary>
        /// Gets the peer data of the node itself.
        /// </summary>
        TKPeerData SelfData { get; }

        /// <summary>
        /// Updates the reference to the node within the table.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="peerData"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask TouchAsync(in TKNodeId nodeId, in TKPeerData peerData, CancellationToken cancellationToken = default);

    }

}
