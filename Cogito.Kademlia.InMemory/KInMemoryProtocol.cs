using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Cogito.Kademlia.Net;
using Cogito.Linq;

using Microsoft.Extensions.Logging;

namespace Cogito.Kademlia.InMemory
{

    /// <summary>
    /// Implements an in-memory protocol, allowing the connection of multiple engines.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public class KInMemoryProtocol<TKNodeId> : IKInMemoryProtocol<TKNodeId>, IKInMemoryProtocolResourceProvider<TKNodeId>
        where TKNodeId : unmanaged
    {

        readonly IKEngine<TKNodeId> engine;
        readonly IKInMemoryProtocolBroker<TKNodeId> broker;
        readonly ILogger logger;

        readonly KInMemoryProtocolEndpoint<TKNodeId> self;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="broker"></param>
        /// <param name="listen"></param>
        public KInMemoryProtocol(IKEngine<TKNodeId> engine, IKInMemoryProtocolBroker<TKNodeId> broker, ILogger logger = null)
        {
            this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
            this.broker = broker ?? throw new ArgumentNullException(nameof(broker));
            this.logger = logger;

            self = CreateEndpoint(engine.SelfId);
        }

        /// <summary>
        /// Gets the ID of this node.
        /// </summary>
        public TKNodeId SelfId => engine.SelfId;

        /// <summary>
        /// Gets the set of endpoints through which this protocol is reachable.
        /// </summary>
        public IEnumerable<IKEndpoint<TKNodeId>> Endpoints => self.Yield();

        /// <summary>
        /// Creates a new <see cref="KIpProtocolEndpoint{TKNodeId}"/> for the given <see cref="KIpEndpoint"/>.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public KInMemoryProtocolEndpoint<TKNodeId> CreateEndpoint(in TKNodeId node)
        {
            return new KInMemoryProtocolEndpoint<TKNodeId>(this, node);
        }

        /// <summary>
        /// Creates a new <see cref="KInMemoryProtocolEndpoint{TKNodeId}"/> for the given <see cref="TKNodeId"/>.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        IKEndpoint<TKNodeId> IKInMemoryProtocolResourceProvider<TKNodeId>.CreateEndpoint(in TKNodeId node)
        {
            return CreateEndpoint(node);
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
        public ValueTask<KResponse<TKNodeId, KPingResponse<TKNodeId>>> PingAsync(IKEndpoint<TKNodeId> target, in KPingRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            if (target is KInMemoryProtocolEndpoint<TKNodeId> t)
                return broker.PingAsync(engine.SelfId, t, request, cancellationToken);

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Invoked to send a STORE request.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TKNodeId, KStoreResponse<TKNodeId>>> StoreAsync(IKEndpoint<TKNodeId> target, in KStoreRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            if (target is KInMemoryProtocolEndpoint<TKNodeId> t)
                return broker.StoreAsync(engine.SelfId, t, request, cancellationToken);

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Invoked to send a FIND_NODE request.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>> FindNodeAsync(IKEndpoint<TKNodeId> target, in KFindNodeRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            if (target is KInMemoryProtocolEndpoint<TKNodeId> t)
                return broker.FindNodeAsync(engine.SelfId, t, request, cancellationToken);

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Invoked to send a FIND_VALUE request.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TKNodeId, KFindValueResponse<TKNodeId>>> FindValueAsync(IKEndpoint<TKNodeId> target, in KFindValueRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            if (target is KInMemoryProtocolEndpoint<TKNodeId> t)
                return broker.FindValueAsync(engine.SelfId, t, request, cancellationToken);

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Invoked when a PING request is received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KPingResponse<TKNodeId>> PingRequestAsync(in TKNodeId sender, KInMemoryProtocolEndpoint<TKNodeId> source, in KPingRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            return engine.OnPingAsync(sender, source, request, cancellationToken);
        }

        /// <summary>
        /// Invoked when a STORE request is received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="source"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KStoreResponse<TKNodeId>> StoreRequestAsync(in TKNodeId sender, KInMemoryProtocolEndpoint<TKNodeId> source, in KStoreRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            return engine.OnStoreAsync(sender, source, request, cancellationToken);
        }

        /// <summary>
        /// Invoked when a FIND_NODE request is received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="source"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KFindNodeResponse<TKNodeId>> FindNodeRequestAsync(in TKNodeId sender, KInMemoryProtocolEndpoint<TKNodeId> source, in KFindNodeRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            return engine.OnFindNodeAsync(sender, source, request, cancellationToken);
        }

        /// <summary>
        /// Invoked when a FIND_VALUE request is received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="source"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KFindValueResponse<TKNodeId>> FindValueRequestAsync(in TKNodeId sender, KInMemoryProtocolEndpoint<TKNodeId> source, in KFindValueRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            return engine.OnFindValueAsync(sender, source, request, cancellationToken);
        }

        /// <summary>
        /// Invoked when a member is registered.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public ValueTask OnMemberRegisteredAsync(TKNodeId node)
        {
            return new ValueTask(Task.CompletedTask);
        }

        /// <summary>
        /// Invoked when a member is unregistered.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public ValueTask OnMemberUnregisteredAsync(TKNodeId node)
        {
            return new ValueTask(Task.CompletedTask);
        }

    }

}
