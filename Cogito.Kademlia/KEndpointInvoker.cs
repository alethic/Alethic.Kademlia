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
        readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="data"></param>
        /// <param name="logger"></param>
        public KEndpointInvoker(in TKNodeId self, TKPeerData data, ILogger logger = null)
        {
            this.self = self;
            this.data = data;
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Attempts to execute a PING request against each of the provided endpoints.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TKNodeId, KPingResponse<TKNodeId>>> PingAsync(IKEndpointSet<TKNodeId> endpoints, CancellationToken cancellationToken = default)
        {
            var r = new KPingRequest<TKNodeId>(data.Endpoints.ToArray());
            return TryAsync(endpoints, ep => ep.PingAsync(r, cancellationToken), cancellationToken);
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
        public ValueTask<KResponse<TKNodeId, KStoreResponse<TKNodeId>>> StoreAsync(IKEndpointSet<TKNodeId> endpoints, TKNodeId key, ReadOnlyMemory<byte>? value, DateTimeOffset? expiration, CancellationToken cancellationToken = default)
        {
            var r = new KStoreRequest<TKNodeId>(key, value, expiration);
            return TryAsync(endpoints, ep => ep.StoreAsync(r, cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Attempts to execute a FIND_NODE request against each of the provided endpoints.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>> FindNodeAsync(IKEndpointSet<TKNodeId> endpoints, TKNodeId key, CancellationToken cancellationToken = default)
        {
            var r = new KFindNodeRequest<TKNodeId>(key);
            return TryAsync(endpoints, ep => ep.FindNodeAsync(r, cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Attempts to execute a FIND_VALUE request against each of the provided endpoints.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TKNodeId, KFindValueResponse<TKNodeId>>> FindValueAsync(IKEndpointSet<TKNodeId> endpoints, TKNodeId key, CancellationToken cancellationToken = default)
        {
            var r = new KFindValueRequest<TKNodeId>(key);
            return TryAsync(endpoints, ep => ep.FindValueAsync(r, cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Attempts to execute the specified method against the provided endpoints.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="func"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KResponse<TKNodeId, TResponseBody>> TryAsync<TResponseBody>(IKEndpointSet<TKNodeId> endpoints, Func<IKEndpoint<TKNodeId>, ValueTask<KResponse<TKNodeId, TResponseBody>>> func, CancellationToken cancellationToken)
            where TResponseBody : struct, IKResponseData<TKNodeId>
        {
            while (cancellationToken.IsCancellationRequested == false && endpoints.Acquire() is IKEndpoint<TKNodeId> endpoint)
            {
                var r = await TryAsync(endpoint, func);
                if (r.Status == KResponseStatus.Success)
                    return r;
            }

            return default;
        }

        /// <summary>
        /// Attempts to execute the specified method against an endpoint.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        async ValueTask<KResponse<TKNodeId, TResponseBody>> TryAsync<TResponseBody>(IKEndpoint<TKNodeId> endpoint, Func<IKEndpoint<TKNodeId>, ValueTask<KResponse<TKNodeId, TResponseBody>>> func)
            where TResponseBody : struct, IKResponseData<TKNodeId>
        {
            try
            {
                logger?.LogTrace("Attempting request against {Endpoint}.", endpoint);
                var r = await func(endpoint);
                if (r.Status == KResponseStatus.Success)
                {
                    logger?.LogTrace("Success contacting {Endpoint}.", endpoint);
                    endpoint.OnSuccess(new KEndpointSuccessEventArgs());
                    return r;
                }
            }
            catch (KProtocolException e) when (e.Error == KProtocolError.EndpointNotAvailable)
            {
                logger?.LogWarning("Endpoint not available: {Endpoint}.", endpoint);
                endpoint.OnTimeout(new KEndpointTimeoutEventArgs());
            }

            return default;
        }

    }

}
