using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Implements an invocation policy.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public class KInvokerPolicy<TNodeId> : IKInvokerPolicy<TNodeId>
        where TNodeId : unmanaged
    {

        static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(1);

        readonly ILogger logger;
        readonly TimeSpan defaultTimeout;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="defaultTimeout"></param>
        public KInvokerPolicy(ILogger logger, TimeSpan? defaultTimeout = null)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.defaultTimeout = defaultTimeout ?? DefaultTimeout;
        }

        /// <summary>
        /// Attempts to execute the specified method against the provided endpoints.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TNodeId, TResponse>> InvokeAsync<TRequest, TResponse>(KEndpointSet<TNodeId> endpoints, in TRequest request, CancellationToken cancellationToken)
            where TRequest : struct, IKRequestBody<TNodeId>
            where TResponse : struct, IKResponseBody<TNodeId>
        {
            return InvokeAsync<TRequest, TResponse>(endpoints, request, cancellationToken);
        }

        /// <summary>
        /// Attempts to execute the specified method against the provided endpoints.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KResponse<TNodeId, TResponse>> InvokeAsync<TRequest, TResponse>(KEndpointSet<TNodeId> endpoints, TRequest request, CancellationToken cancellationToken)
            where TRequest : struct, IKRequestBody<TNodeId>
            where TResponse : struct, IKResponseBody<TNodeId>
        {
            // replace token with linked timeout
            cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(new CancellationTokenSource(defaultTimeout).Token, cancellationToken).Token;

            // continue until timeout
            while (cancellationToken.IsCancellationRequested == false && endpoints.Acquire() is IKProtocolEndpoint<TNodeId> endpoint)
            {
                var r = await TryAsync<TRequest, TResponse>(endpoints, endpoint, request, cancellationToken);
                if (r.Status == KResponseStatus.Success)
                    return r;
            }

            return default;
        }

        /// <summary>
        /// Attempts to execute the specified method against an endpoint.IO
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        async ValueTask<KResponse<TNodeId, TResponseBody>> TryAsync<TRequestBody, TResponseBody>(KEndpointSet<TNodeId> endpoints, IKProtocolEndpoint<TNodeId> endpoint, TRequestBody request, CancellationToken cancellationToken)
            where TRequestBody : struct, IKRequestBody<TNodeId>
            where TResponseBody : struct, IKResponseBody<TNodeId>
        {
            try
            {
                logger?.LogTrace("Attempting request against {Endpoint}.", endpoint);
                var r = await endpoint.InvokeAsync<TRequestBody, TResponseBody>(request, cancellationToken);
                if (r.Status == KResponseStatus.Success)
                {
                    logger.LogTrace("Success contacting {Endpoint}.", endpoint);
                    endpoints.Update(endpoint);
                    return r;
                }
                else
                {
                    logger.LogWarning("Failure from endpoint: {Endpoint}.", endpoint);
                    endpoints.Insert(endpoint);
                }
            }
            catch (KProtocolException e) when (e.Error == KProtocolError.EndpointNotAvailable)
            {
                logger.LogWarning("Endpoint not available: {Endpoint}.", endpoint);
                endpoints.Insert(endpoint);
            }

            return default;
        }

    }

}
