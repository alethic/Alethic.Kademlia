using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Cogito.Kademlia.Network;
using Cogito.Linq;

using Microsoft.Extensions.Logging;

namespace Cogito.Kademlia.InMemory
{

    /// <summary>
    /// Implements an in-memory protocol, allowing the connection of multiple engines.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public class KInMemoryProtocol<TNodeId> : IKInMemoryProtocol<TNodeId>
        where TNodeId : unmanaged
    {

        readonly IKEngine<TNodeId> engine;
        readonly IKRequestHandler<TNodeId> handler;
        readonly IKInMemoryProtocolBroker<TNodeId> broker;
        readonly ILogger logger;

        readonly KInMemoryProtocolEndpoint<TNodeId> self;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="handler"></param>
        /// <param name="broker"></param>
        /// <param name="logger"></param>
        public KInMemoryProtocol(IKEngine<TNodeId> engine, IKRequestHandler<TNodeId> handler, IKInMemoryProtocolBroker<TNodeId> broker, ILogger logger)
        {
            this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
            this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
            this.broker = broker ?? throw new ArgumentNullException(nameof(broker));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            self = CreateEndpoint(engine.SelfId);
        }

        /// <summary>
        /// Gets the ID of this node.
        /// </summary>
        public TNodeId SelfId => engine.SelfId;

        /// <summary>
        /// Gets the set of endpoints through which this protocol is reachable.
        /// </summary>
        public IEnumerable<IKProtocolEndpoint<TNodeId>> Endpoints => self.Yield();

        /// <summary>
        /// Creates a new <see cref="KIpProtocolEndpoint{TNodeId}"/> for the given <see cref="KIpEndpoint"/>.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public KInMemoryProtocolEndpoint<TNodeId> CreateEndpoint(in TNodeId node)
        {
            return new KInMemoryProtocolEndpoint<TNodeId>(this, node, Enumerable.Empty<string>());
        }

        /// <summary>
        /// Creates a new <see cref="KInMemoryProtocolEndpoint{TNodeId}"/> for the given <see cref="Uri"/>.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public unsafe IKProtocolEndpoint<TNodeId> ResolveEndpoint(Uri uri)
        {
            if (uri.Scheme == "memory")
            {
                var b = (Span<byte>)stackalloc byte[KNodeId<TNodeId>.SizeOf];
                WriteHexString(uri.Host, b);
                return new KInMemoryProtocolEndpoint<TNodeId>(this, KNodeId<TNodeId>.Read(b), Enumerable.Empty<string>());
            }

            return null;
        }

        /// <summary>
        /// Writes the hex string to the output span.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="destination"></param>
        static void WriteHexString(string value, Span<byte> destination)
        {
            static int GetHex(char v) => v - (v < 58 ? 48 : (v < 97 ? 55 : 87));

            if (value.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            for (var i = 0; i < value.Length >> 1; ++i)
                destination[i] = (byte)((GetHex(value[i << 1]) << 4) + (GetHex(value[(i << 1) + 1])));
        }

        /// <summary>
        /// Starts the protocol.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            return broker.RegisterAsync(engine.SelfId, this, cancellationToken).AsTask();
        }

        /// <summary>
        /// Stops the protocol.
        /// </summary>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            return broker.UnregisterAsync(engine.SelfId, this, cancellationToken).AsTask();
        }

        /// <summary>
        /// Invoked to send a PING request.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TNodeId, TResponse>> InvokeAsync<TRequest, TResponse>(IKProtocolEndpoint<TNodeId> target, in TRequest request, CancellationToken cancellationToken)
            where TRequest : struct, IKRequestBody<TNodeId>
            where TResponse : struct, IKResponseBody<TNodeId>
        {
            if (target is KInMemoryProtocolEndpoint<TNodeId> t)
                return broker.InvokeAsync<TRequest, TResponse>(engine.SelfId, t, request, cancellationToken);

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Invoked when a PING request is received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<TResponse> InvokeRequestAsync<TRequest, TResponse>(in TNodeId sender, KInMemoryProtocolEndpoint<TNodeId> source, in TRequest request, CancellationToken cancellationToken)
            where TRequest : struct, IKRequestBody<TNodeId>
            where TResponse : struct, IKResponseBody<TNodeId>
        {
            return InvokeRequestAsync<TRequest, TResponse>(sender, source, request, cancellationToken);
        }

        /// <summary>
        /// Invoked when a PING request is received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="source"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async ValueTask<TResponse> InvokeRequestAsync<TRequest, TResponse>(TNodeId sender, KInMemoryProtocolEndpoint<TNodeId> source, TRequest request, CancellationToken cancellationToken)
            where TRequest : struct, IKRequestBody<TNodeId>
            where TResponse : struct, IKResponseBody<TNodeId>
        {
            switch (request)
            {
                case KPingRequest<TNodeId> ping:
                    return (TResponse)(IKResponseBody<TNodeId>)await handler.OnPingAsync(sender, ping, cancellationToken);
                case KStoreRequest<TNodeId> store:
                    return (TResponse)(IKResponseBody<TNodeId>)await handler.OnStoreAsync(sender, store, cancellationToken);
                case KFindNodeRequest<TNodeId> findNode:
                    return (TResponse)(IKResponseBody<TNodeId>)await handler.OnFindNodeAsync(sender, findNode, cancellationToken);
                case KFindValueRequest<TNodeId> findValue:
                    return (TResponse)(IKResponseBody<TNodeId>)await handler.OnFindValueAsync(sender, findValue, cancellationToken);
                default:
                    throw new KProtocolException(KProtocolError.Invalid, "Invalid message type.");
            }
        }

        /// <summary>
        /// Invoked when a member is registered.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public ValueTask OnMemberRegisteredAsync(TNodeId node)
        {
            return new ValueTask(Task.CompletedTask);
        }

        /// <summary>
        /// Invoked when a member is unregistered.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public ValueTask OnMemberUnregisteredAsync(TNodeId node)
        {
            return new ValueTask(Task.CompletedTask);
        }

    }

}
