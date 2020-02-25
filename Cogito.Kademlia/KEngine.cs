using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Cogito.Threading;

using Microsoft.Extensions.Logging;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Provides an implementation of a Kademlia network engine. The <see cref="KEngine{TKNodeId, TKPeerData}"/>
    /// class implements the core runtime logic of a Kademlia node.
    /// </summary>
    public class KEngine<TKNodeId, TKPeerData> : IKEngine<TKNodeId, TKPeerData>, IKDistributedTable<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
        where TKPeerData : IKEndpointProvider<TKNodeId>
    {

        readonly IKRouter<TKNodeId, TKPeerData> router;
        readonly IKEndpointInvoker<TKNodeId> invoker;
        readonly IKLookup<TKNodeId> lookup;
        readonly IKStore<TKNodeId> store;
        readonly ILogger logger;
        readonly AsyncLock sync = new AsyncLock();

        CancellationTokenSource runCts;
        Task run;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="router"></param>
        /// <param name="invoker"></param>
        /// <param name="lookup"></param>
        /// <param name="store"></param>
        /// <param name="logger"></param>
        public KEngine(IKRouter<TKNodeId, TKPeerData> router, IKEndpointInvoker<TKNodeId> invoker, IKLookup<TKNodeId> lookup, IKStore<TKNodeId> store, ILogger logger = null)
        {
            this.router = router ?? throw new ArgumentNullException(nameof(router));
            this.invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
            this.lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.logger = logger;
        }

        /// <summary>
        /// Gets the Node ID of the node itself.
        /// </summary>
        public TKNodeId SelfId => router.SelfId;

        /// <summary>
        /// Gets the peer data of the node itself.
        /// </summary>
        public TKPeerData SelfData => router.SelfData;

        /// <summary>
        /// Gets the router associated with the engine.
        /// </summary>
        public IKRouter<TKNodeId, TKPeerData> Router => router;

        /// <summary>
        /// Gets the lookup engine that provides for node traversal.
        /// </summary>
        public IKLookup<TKNodeId> Lookup => lookup;

        /// <summary>
        /// Initiates a bootstrap connection to the specified endpoints.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async ValueTask ConnectAsync(IEnumerable<IKEndpoint<TKNodeId>> endpoints, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Bootstrapping network with connection to {Endpoints}.", endpoints);

            // ping node, which ensures availability and populates tables upon response
            var r = await invoker.PingAsync(endpoints, cancellationToken);
            if (r.Status == KResponseStatus.Failure)
                throw new KProtocolException(KProtocolError.EndpointNotAvailable, "Unable to bootstrap off of the specified endpoints. No response.");

            await lookup.LookupNodeAsync(SelfId, cancellationToken);
        }

        /// <summary>
        /// Initiates a bootstrap connection to the specified endpoint.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask ConnectAsync(IKEndpoint<TKNodeId> endpoint, CancellationToken cancellationToken = default)
        {
            return ConnectAsync(new[] { endpoint });
        }

        /// <summary>
        /// Initiates a refresh of the Kademlia network by initiating lookups for random node IDs different distances from
        /// the current node ID.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask RefreshAsync(CancellationToken cancellationToken = default)
        {
            return new ValueTask(Task.WhenAll(Enumerable.Range(1, KNodeId<TKNodeId>.SizeOf * 8 - 1).Select(i => KNodeId<TKNodeId>.Randomize(SelfId, i)).Select(i => lookup.LookupNodeAsync(i, cancellationToken).AsTask())));
        }

        /// <summary>
        /// Gets the value for the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<ReadOnlyMemory<byte>?> GetValueAsync(in TKNodeId key, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the value for the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask SetValueAsync(in TKNodeId key, ReadOnlyMemory<byte>? value, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Starts the processes of the engine.
        /// </summary>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            using (await sync.LockAsync())
            {
                if (run != null || runCts != null)
                    throw new InvalidOperationException();

                // begin new run processes
                runCts = new CancellationTokenSource();
                run = Task.WhenAll(Task.Run(() => PeriodicRefreshAsync(runCts.Token)));
            }
        }

        /// <summary>
        /// Stops the processes of the engine.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            using (await sync.LockAsync())
            {
                if (runCts != null)
                {
                    runCts.Cancel();
                    runCts = null;
                }

                if (run != null)
                {
                    try
                    {
                        await run;
                    }
                    catch (OperationCanceledException)
                    {
                        // ignore
                    }
                }
            }
        }

        /// <summary>
        /// Periodically refreshes the Kademlia tables until told to exit
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task PeriodicRefreshAsync(CancellationToken cancellationToken)
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                await RefreshAsync(cancellationToken);
                await Task.Delay(TimeSpan.FromMinutes(60), cancellationToken);
            }
        }

        /// <summary>
        /// Invoked to handle incoming PING requests.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KPingResponse<TKNodeId>> IKEngine<TKNodeId>.OnPingAsync(in TKNodeId sender, IKEndpoint<TKNodeId> endpoint, in KPingRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            logger?.LogDebug("Processing {Operation} from {Sender}.", "PING", sender);
            return OnPingAsync(sender, endpoint, request, cancellationToken);
        }

        /// <summary>
        /// Invoked to handle incoming PING requests.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KPingResponse<TKNodeId>> OnPingAsync(TKNodeId sender, IKEndpoint<TKNodeId> endpoint, KPingRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            await router.UpdatePeerAsync(sender, endpoint, request.Endpoints, cancellationToken);
            return request.Respond(SelfData.Endpoints.ToArray());
        }

        /// <summary>
        /// Invoked to handle incoming STORE requests.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KStoreResponse<TKNodeId>> IKEngine<TKNodeId>.OnStoreAsync(in TKNodeId sender, IKEndpoint<TKNodeId> endpoint, in KStoreRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            logger?.LogDebug("Processing {Operation} from {Sender}.", "STORE", sender);
            return OnStoreAsync(sender, endpoint, request, cancellationToken);
        }

        /// <summary>
        /// Invoked to handle incoming STORE requests.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KStoreResponse<TKNodeId>> OnStoreAsync(TKNodeId source, IKEndpoint<TKNodeId> endpoint, KStoreRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            await router.UpdatePeerAsync(source, endpoint, null, cancellationToken);
            await store.SetAsync(request.Key, request.Value, request.Expiration);
            return request.Respond();
        }

        /// <summary>
        /// Invoked to handle incoming FIND_NODE requests.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KFindNodeResponse<TKNodeId>> IKEngine<TKNodeId>.OnFindNodeAsync(in TKNodeId sender, IKEndpoint<TKNodeId> endpoint, in KFindNodeRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            logger?.LogDebug("Processing {Operation} from {Sender}.", "FIND_NODE", sender);
            return OnFindNodeAsync(sender, endpoint, request, cancellationToken);
        }

        /// <summary>
        /// Invoked to handle incoming FIND_NODE requests.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KFindNodeResponse<TKNodeId>> OnFindNodeAsync(TKNodeId sender, IKEndpoint<TKNodeId> endpoint, KFindNodeRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            await router.UpdatePeerAsync(sender, endpoint, null, cancellationToken);
            return request.Respond(await router.GetNextHopAsync(request.Key, router.K, cancellationToken));
        }

        /// <summary>
        /// Invoked to handle incoming FIND_VALUE requests.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KFindValueResponse<TKNodeId>> IKEngine<TKNodeId>.OnFindValueAsync(in TKNodeId sender, IKEndpoint<TKNodeId> endpoint, in KFindValueRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            logger?.LogDebug("Processing {Operation} from {Sender}.", "FIND_VALUE", sender);
            return OnFindValueAsync(sender, endpoint, request, cancellationToken);
        }

        /// <summary>
        /// Invoked to handle incoming FIND_VALUE requests.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KFindValueResponse<TKNodeId>> OnFindValueAsync(TKNodeId sender, IKEndpoint<TKNodeId> endpoint, KFindValueRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            await router.UpdatePeerAsync(sender, endpoint, null, cancellationToken);
            var r = await store.GetAsync(request.Key);
            return request.Respond(await router.GetNextHopAsync(request.Key, router.K, cancellationToken), r.Value, r.Expiration); // TODO respond with correct info
        }

    }

}
