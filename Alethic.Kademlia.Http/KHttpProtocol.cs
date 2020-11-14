using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Alethic.Kademlia.Http
{

    /// <summary>
    /// Implements a HTTP protocol for Kademlia.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public class KHttpProtocol<TNodeId> : IKProtocol<TNodeId>, IKService
        where TNodeId : unmanaged
    {

        readonly IKHost<TNodeId> host;
        readonly IKRequestHandler<TNodeId> handler;
        readonly ITypedHttpClientFactory<KHttpProtocol<TNodeId>> http;
        readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="handler"></param>
        /// <param name="http"></param>
        /// <param name="logger"></param>
        public KHttpProtocol(IKHost<TNodeId> host, IKRequestHandler<TNodeId> handler, ITypedHttpClientFactory<KHttpProtocol<TNodeId>> http, ILogger logger)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
            this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
            this.http = http ?? throw new ArgumentNullException(nameof(http));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Attempts to resolve the given URI to an endpoint.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public IKProtocolEndpoint<TNodeId> ResolveEndpoint(Uri uri)
        {
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps ? new KHttpProtocolEndpoint<TNodeId>(this, uri) : null;
        }

        /// <summary>
        /// Starts the network.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            host.RegisterProtocol(this);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops the network.
        /// </summary>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            host.UnregisterProtocol(this);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Adds an an endpoint to the protocol.
        /// </summary>
        /// <param name="uri"></param>
        public void RegisterListenUri(Uri uri)
        {
            host.RegisterEndpoint(uri);
        }

        /// <summary>
        /// Removes an endpoint from the protocol.
        /// </summary>
        /// <param name="uri"></param>
        public void UnregisterListenUri(Uri uri)
        {
            host.UnregisterEndpoint(uri);
        }

        /// <summary>
        /// Handles an outgoing HTTP request.
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="target"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal ValueTask<KResponse<TNodeId, TResponse>> InvokeAsync<TRequest, TResponse>(KHttpProtocolEndpoint<TNodeId> target, in TRequest request, CancellationToken cancellationToken = default)
            where TRequest : struct, IKRequestBody<TNodeId>
            where TResponse : struct, IKResponseBody<TNodeId>
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Handles an incoming HTTP request.
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IKResponse<TNodeId>> ReceiveAsync(IKRequest<TNodeId> request, CancellationToken cancellationToken)
        {
            return request switch
            {
                IKRequest<TNodeId, KPingRequest<TNodeId>> ping => CreateResponse(ping, await handler.OnPingAsync(ping.Header.Sender, ping.Body.Value, cancellationToken)),
                IKRequest<TNodeId, KStoreRequest<TNodeId>> store => CreateResponse(store, await handler.OnStoreAsync(store.Header.Sender, store.Body.Value, cancellationToken)),
                IKRequest<TNodeId, KFindNodeRequest<TNodeId>> findNode => CreateResponse(findNode, await handler.OnFindNodeAsync(findNode.Header.Sender, findNode.Body.Value, cancellationToken)),
                IKRequest<TNodeId, KFindValueRequest<TNodeId>> findValue => CreateResponse(findValue, await handler.OnFindValueAsync(findValue.Header.Sender, findValue.Body.Value, cancellationToken)),
                _ => throw new InvalidOperationException(),
            };
        }

        /// <summary>
        /// Packages the response.
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        KResponse<TNodeId, TResponse> CreateResponse<TRequest, TResponse>(IKRequest<TNodeId, TRequest> request, TResponse response)
            where TRequest : struct, IKRequestBody<TNodeId>
            where TResponse : struct, IKResponseBody<TNodeId>
        {
            try
            {
                return new KResponse<TNodeId, TResponse>(new KMessageHeader<TNodeId>(host.SelfId, request.Header.ReplyId), KResponseStatus.Success, response);
            }
            catch
            {
                return new KResponse<TNodeId, TResponse>(new KMessageHeader<TNodeId>(host.SelfId, request.Header.ReplyId), KResponseStatus.Failure, null);
            }
        }

    }

}