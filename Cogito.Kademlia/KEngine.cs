using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Cogito.Kademlia.Core;

using Microsoft.Extensions.Logging;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Provides an implementation of a Kademlia network engine. The <see cref="KEngine{TKNodeId, TKPeerData}"/>
    /// class implements the core runtime logic of a Kademlia node.
    /// </summary>
    public class KEngine<TKNodeId, TKPeerData> : IKEngine<TKNodeId, TKPeerData>, IKDistributedTable<TKNodeId>, IKLookup<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
        where TKPeerData : IKEndpointProvider<TKNodeId>
    {

        readonly int alpha;
        readonly IKRouter<TKNodeId, TKPeerData> router;
        readonly IKEndpointInvoker<TKNodeId> invoker;
        readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="router"></param>
        /// <param name="invoker"></param>
        /// <param name="alpha"></param>
        /// <param name="logger"></param>
        public KEngine(IKRouter<TKNodeId, TKPeerData> router, IKEndpointInvoker<TKNodeId> invoker, int alpha = 3, ILogger logger = null)
        {
            if (router == null)
                throw new ArgumentNullException(nameof(router));
            if (alpha < 1)
                throw new ArgumentOutOfRangeException(nameof(alpha));

            this.router = router;
            this.alpha = alpha;
            this.invoker = invoker;
            this.logger = logger;

            // configure router
            this.router.Engine = this;
            logger?.LogInformation("Attached to router as {NodeId}.", this.router.SelfId);
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
        /// Gets the 'alpha' value. The alpha value represents the number of concurrent requests to keep inflight for
        /// a lookup.
        /// </summary>
        public int Alpha => alpha;

        /// <summary>
        /// Initiates a bootstrap connection to the specified endpoints.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async ValueTask ConnectAsync(IEnumerable<IKEndpoint<TKNodeId>> endpoints, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Bootstrapping network with connection to {Endpoints}.", endpoints);

            var r = await invoker.PingAsync(endpoints, cancellationToken);
            if (r.Status == KResponseStatus.Failure)
                throw new KProtocolException(KProtocolError.EndpointNotAvailable, "Unable to bootstrap off of the specified endpoints. No response.");

            await router.UpdatePeerAsync(r.Sender, null, r.Body.Endpoints, cancellationToken);
            await LookupAsync(SelfId, cancellationToken);
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
        /// Initiates a lookup for the specified key, returning the closest discovered node.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KLookupResult<TKNodeId>> LookupNodeAsync(in TKNodeId key, CancellationToken cancellationToken = default)
        {
            return LookupAsync(key, cancellationToken);
        }

        /// <summary>
        /// Initiates a lookup for the specified key, returning the closest discovered node.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KLookupResult<TKNodeId>> LookupValueAsync(in TKNodeId key, CancellationToken cancellationToken = default)
        {
            return LookupAsync(key, cancellationToken);
        }

        /// <summary>
        /// Begins a search process for the specified node.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KLookupResult<TKNodeId>> LookupAsync(TKNodeId key, CancellationToken cancellationToken = default)
        {
            var wait = new HashSet<Task<IEnumerable<KPeerEndpointInfo<TKNodeId>>>>();
            var comp = new KNodeIdDistanceComparer<TKNodeId>(key);

            // kill is used to cancel outstanding tasks early
            var kill = new CancellationTokenSource();
            var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, kill.Token);

            // find our own closest peers to seed from
            var init = await router.GetPeersAsync(key, alpha, cancellationToken);

            // tracks the peers remaining to query sorted by distance
            var todo = new C5.IntervalHeap<KPeerEndpointInfo<TKNodeId>>(router.K, new FuncComparer<KPeerEndpointInfo<TKNodeId>, TKNodeId>(i => i.Id, comp));
            todo.AddAll(init);

            // track done nodes so we don't recurse; and maintain a list of near nodes that have been traversed
            var done = new HashSet<TKNodeId>(todo.Select(i => i.Id));
            var path = new C5.IntervalHeap<KPeerEndpointInfo<TKNodeId>>(router.K, new FuncComparer<KPeerEndpointInfo<TKNodeId>, TKNodeId>(i => i.Id, comp));

            try
            {
                // continue until all work is completed
                while (todo.Count > 0 || wait.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (path.Count > 0)
                    {
                        // check that the current nearest node isn't the key itself; if so, we are obviously done
                        var near = path.FindMin();
                        if (near.Id.Equals(key))
                            return new KLookupResult<TKNodeId>(key, near, path);
                    }

                    // schedule queries of our closest nodes
                    while (wait.Count < alpha && todo.Count > 0)
                    {
                        // schedule new node to query
                        var n = todo.DeleteMin();
                        if (n.Id.Equals(SelfId) == false)
                            wait.Add(FindNodeAsync(n, key, stop.Token).AsTask().ContinueWith((r, o) => r.Result, n));
                    }

                    // we have at least one task in the task pool to wait for
                    if (wait.Count > 0)
                    {
                        // wait for first finished task
                        var r = await Task.WhenAny(wait);
                        wait.Remove(r);

                        // skip cancelled tasks
                        if (r.IsCanceled)
                            continue;

                        // skip failed tasks
                        if (r.Exception != null)
                        {
                            logger?.LogError(r.Exception, "Received error from lookup task.");
                            continue;
                        }

                        // task returned something; must have succeeded in our lookup
                        if (r.Result != null)
                        {
                            // after we've received a successful result; mark the node as one we've encountered
                            path.Add((KPeerEndpointInfo<TKNodeId>)r.AsyncState);

                            // iterate over newly retrieved peers
                            foreach (var i in r.Result)
                            {
                                // received node is closer than current
                                if (i.Id.Equals(SelfId) == false)
                                {
                                    if (done.Add(i.Id))
                                    {
                                        todo.Add(i);

                                        // remove uninteresting nodes
                                        while (todo.Count > router.K)
                                            todo.DeleteMax();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                // signal any remaining tasks to exit immediately
                kill.Cancel();

                try
                {
                    // clean up and capture results of outstanding
                    if (wait.Count > 0)
                    {
                        logger?.LogDebug("Cancelling {Count} outstanding requests.", wait.Count);
                        await Task.WhenAll(wait);
                    }
                }
                catch (OperationCanceledException)
                {
                    // ignore
                }
            }

            // we never found anything; return the path we took, but that's it
            return new KLookupResult<TKNodeId>(key, null, path);
        }

        /// <summary>
        /// Issues a FIND_NODE request to the peer, looking for the specified key, and returns the resolved peers and their endpoints.
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<IEnumerable<KPeerEndpointInfo<TKNodeId>>> FindNodeAsync(KPeerEndpointInfo<TKNodeId> peer, TKNodeId key, CancellationToken cancellationToken)
        {
            var r = await invoker.FindNodeAsync(peer.Endpoints, key, cancellationToken);
            if (r.Status == KResponseStatus.Success)
                return r.Body.Peers;

            return null;
        }

        /// <summary>
        /// Issues a FIND_NODE request to the peer, looking for the specified key, and returns the resolved peers and their endpoints.
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<(IEnumerable<KPeerEndpointInfo<TKNodeId>>, ReadOnlyMemory<byte>?)> FindValueAsync(KPeerEndpointInfo<TKNodeId> peer, TKNodeId key, CancellationToken cancellationToken)
        {
            var r = await invoker.FindValueAsync(peer.Endpoints, key, cancellationToken);
            if (r.Status == KResponseStatus.Success)
                return (r.Body.Peers, r.Body.Value);

            return (null, null);
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
            return request.Respond(await router.GetPeersAsync(request.Key, router.K, cancellationToken));
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
            return request.Respond(null, await router.GetPeersAsync(request.Key, router.K, cancellationToken)); // TODO respond with correct info
        }

    }

}
