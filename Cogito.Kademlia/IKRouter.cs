using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a Kademlia routing table. A routing table is used to track the known nodes within the network and
    /// provide lookups.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public interface IKRouter<TNodeId> :
#if NETSTANDARD2_1
        IAsyncEnumerable<KNodeEndpointInfo<TNodeId>>,
#endif
        IEnumerable<KNodeEndpointInfo<TNodeId>>
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Gets the current K-value of the router.
        /// </summary>
        int K { get; }

        /// <summary>
        /// Gets the number of peers known by the router.
        /// </summary>
        int Count { get; }

#if NETSTANDARD2_1

        /// <summary>
        /// Gets the data associated with the closest <paramref name="k"/> peers to <paramref name="key"/>.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="k"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        IAsyncEnumerable<KNodeEndpointInfo<TNodeId>> SelectAsync(in TNodeId key, int k, CancellationToken cancellationToken = default);

#else

        /// <summary>
        /// Gets the data associated with the closest <paramref name="k"/> peers to <paramref name="key"/>.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="k"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<IEnumerable<KNodeEndpointInfo<TNodeId>>> SelectAsync(in TNodeId key, int k, CancellationToken cancellationToken = default);

#endif

        /// <summary>
        /// Updates the endpoints of the node within the router.
        /// </summary>
        /// <param name="key">ID of the peer to gain knowledge of.</param>
        /// <param name="endpoints">Additional known endpoints of the peer.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask UpdateAsync(in TNodeId key, IEnumerable<IKProtocolEndpoint<TNodeId>> endpoints, CancellationToken cancellationToken = default);

    }

}
