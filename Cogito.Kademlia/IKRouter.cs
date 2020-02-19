using System.Buffers;
using System.Collections.Generic;
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
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
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
        /// Gets the stored peer data within the router.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<TKPeerData> GetPeerAsync(in TKNodeId nodeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the endpoints of the node within the router.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="endpoints"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask UpdatePeerAsync(in TKNodeId nodeId, IEnumerable<IKEndpoint<TKNodeId>> endpoints, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the <paramref name="k"/> closest peers to the specified target key.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="k"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<IEnumerable<KPeerEndpoints<TKNodeId>>> GetPeersAsync(in TKNodeId target, int k, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the number of peers known by the router.
        /// </summary>
        int Count { get; }

    }

}
