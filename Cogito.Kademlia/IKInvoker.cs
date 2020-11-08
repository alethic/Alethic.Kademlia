using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Provides the capability to invoke a set of endpoints.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public interface IKInvoker<TNodeId>
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Invokes a PING request using the given endpoints.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TNodeId, KPingResponse<TNodeId>>> PingAsync(KProtocolEndpointSet<TNodeId> endpoints, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invokes a STORE request using the given endpoints.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="key"></param>
        /// <param name="mode"></param>
        /// <param name="value"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TNodeId, KStoreResponse<TNodeId>>> StoreAsync(KProtocolEndpointSet<TNodeId> endpoints, in TNodeId key, KStoreRequestMode mode, in KValueInfo? value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invokes a FIND_NODE request using the given endpoints.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TNodeId, KFindNodeResponse<TNodeId>>> FindNodeAsync(KProtocolEndpointSet<TNodeId> endpoints, in TNodeId key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invokes a FIND_VALUE request using the given endpoints.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TNodeId, KFindValueResponse<TNodeId>>> FindValueAsync(KProtocolEndpointSet<TNodeId> endpoints, in TNodeId key, CancellationToken cancellationToken = default);

    }

}
