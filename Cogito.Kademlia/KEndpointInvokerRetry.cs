using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Provides the ability to execute operations across multiple endpoints.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public class KEndpointInvokerRetry<TKNodeId> : IKEndpointInvokerRetry<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="logger"></param>
        public KEndpointInvokerRetry(ILogger logger = null)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Attempts to execute the specified method against the provided endpoints.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public async ValueTask<KResponse<TKNodeId, TResponseBody>> TryAsync<TResponseBody>(IEnumerable<IKEndpoint<TKNodeId>> endpoints, Func<IKEndpoint<TKNodeId>, ValueTask<KResponse<TKNodeId, TResponseBody>>> func)
            where TResponseBody : struct, IKResponseData<TKNodeId>
        {
            foreach (var endpoint in endpoints)
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
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KResponse<TKNodeId, TResponseBody>> TryAsync<TResponseBody>(IKEndpoint<TKNodeId> endpoint, Func<IKEndpoint<TKNodeId>, ValueTask<KResponse<TKNodeId, TResponseBody>>> func)
            where TResponseBody : struct, IKResponseData<TKNodeId>
        {
            try
            {
                logger?.LogTrace("Attempting request against {Endpoint}.", endpoint);
                var r = await func(endpoint);
                if (r.Status == KResponseStatus.Success)
                    return r;
            }
            catch (TimeoutException)
            {
                logger?.LogWarning("Timeout received contacting {Endpoint}.", endpoint);
                endpoint.OnTimeout(new KEndpointTimeoutEventArgs());
            }

            return default;
        }

    }

}
