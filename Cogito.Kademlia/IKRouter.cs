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
    /// <typeparam name="TKNodeData"></typeparam>
    public interface IKRouter<TKNodeId, TKNodeData> : IKRouter<TKNodeId>
        where TKNodeId : unmanaged
    {

        /// <summary>
        /// Gets the peer data of the node itself.
        /// </summary>
        TKNodeData SelfData { get; }

        /// <summary>
        /// Gets the data associated with the specified peer.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<TKNodeData> GetNodeDataAsync(in TKNodeId id, CancellationToken cancellationToken = default);

    }

    /// <summary>
    /// Describes a Kademlia routing table. A routing table is used to track the known nodes within the network and
    /// provide lookups.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public interface IKRouter<TKNodeId>
        where TKNodeId : unmanaged
    {

        /// <summary>
        /// Gets the ID of the node itself.
        /// </summary>
        TKNodeId Self { get; }

        /// <summary>
        /// Gets the current K-value of the router.
        /// </summary>
        int K { get; }

        /// <summary>
        /// Gets the number of peers known by the router.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the endpoint information regarding the closest <paramref name="k"/> peers to <paramref name="key"/> within the router.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="k"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<IEnumerable<KPeerEndpointInfo<TKNodeId>>> SelectPeersAsync(in TKNodeId key, int k, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the endpoints of the node within the router.
        /// </summary>
        /// <param name="id">ID of the peer to gain knowledge of.</param>
        /// <param name="endpoints">Additional known endpoints of the peer.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask UpdatePeerAsync(in TKNodeId id, IEnumerable<IKEndpoint<TKNodeId>> endpoints, CancellationToken cancellationToken = default);

    }

}
