using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a component that can invoke endpoint methods across a set of multiple endpoints.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public interface IKEndpointInvoker<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Attempts to execute a PING request against each of the provided endpoints.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KResponse<TKNodeId, KPingResponse<TKNodeId>>> PingAsync(IKEndpointSet<TKNodeId> endpoints, CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempts to execute a STORE request against each of the provided endpoints.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiration"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KResponse<TKNodeId, KStoreResponse<TKNodeId>>> StoreAsync(IKEndpointSet<TKNodeId> endpoints, TKNodeId key, ReadOnlyMemory<byte>? value, DateTimeOffset? expiration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempts to execute a FIND_NODE request against each of the provided endpoints.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>> FindNodeAsync(IKEndpointSet<TKNodeId> endpoints, TKNodeId key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempts to execute a FIND_VALUE request against each of the provided endpoints.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KResponse<TKNodeId, KFindValueResponse<TKNodeId>>> FindValueAsync(IKEndpointSet<TKNodeId> endpoints, TKNodeId key, CancellationToken cancellationToken = default);

    }

}
