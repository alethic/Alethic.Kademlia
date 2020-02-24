using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Provides the high level operations against endpoints.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    /// <typeparam name="TKPeerData"></typeparam>
    public class KEndpointInvoker<TKNodeId, TKPeerData> : IKEndpointInvoker<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
        where TKPeerData : IKEndpointProvider<TKNodeId>
    {

        readonly TKNodeId self;
        readonly TKPeerData data;
        readonly IKEndpointInvokerRetry<TKNodeId> retry;
        readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="data"></param>
        /// <param name="retry"></param>
        /// <param name="logger"></param>
        public KEndpointInvoker(in TKNodeId self, TKPeerData data, IKEndpointInvokerRetry<TKNodeId> retry = null, ILogger logger = null)
        {
            this.self = self;
            this.data = data;
            this.retry = retry ?? new KEndpointInvokerRetry<TKNodeId>(logger);
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Attempts to execute a PING request against each of the provided endpoints.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TKNodeId, KPingResponse<TKNodeId>>> PingAsync(IEnumerable<IKEndpoint<TKNodeId>> endpoints, CancellationToken cancellationToken = default)
        {
            var r = new KPingRequest<TKNodeId>(data.Endpoints.ToArray());
            return retry.TryAsync(endpoints, ep => ep.PingAsync(r, cancellationToken));
        }

        /// <summary>
        /// Attempts to execute a STORE request against each of the provided endpoints.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiration"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TKNodeId, KStoreResponse<TKNodeId>>> StoreAsync(IEnumerable<IKEndpoint<TKNodeId>> endpoints, TKNodeId key, ReadOnlyMemory<byte>? value, DateTimeOffset? expiration, CancellationToken cancellationToken = default)
        {
            var r = new KStoreRequest<TKNodeId>(key, value, expiration);
            return retry.TryAsync(endpoints, ep => ep.StoreAsync(r, cancellationToken));
        }

        /// <summary>
        /// Attempts to execute a FIND_NODE request against each of the provided endpoints.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>> FindNodeAsync(IEnumerable<IKEndpoint<TKNodeId>> endpoints, TKNodeId key, CancellationToken cancellationToken = default)
        {
            var r = new KFindNodeRequest<TKNodeId>(key);
            return retry.TryAsync(endpoints, ep => ep.FindNodeAsync(r, cancellationToken));
        }

        /// <summary>
        /// Attempts to execute a FIND_VALUE request against each of the provided endpoints.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TKNodeId, KFindValueResponse<TKNodeId>>> FindValueAsync(IEnumerable<IKEndpoint<TKNodeId>> endpoints, TKNodeId key, CancellationToken cancellationToken = default)
        {
            var r = new KFindValueRequest<TKNodeId>(key);
            return retry.TryAsync(endpoints, ep => ep.FindValueAsync(r, cancellationToken));
        }

    }

}
