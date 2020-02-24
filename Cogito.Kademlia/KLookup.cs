using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Cogito.Kademlia.Core;

using Microsoft.Extensions.Logging;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Provides the node lookup operation logic against a <see cref="IKRouter{TKNodeId}"/>.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public class KLookup<TKNodeId> : IKLookup<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly IKRouter<TKNodeId> router;
        readonly IKEndpointInvoker<TKNodeId> invoker;
        readonly int alpha;
        readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="router"></param>
        /// <param name="invoker"></param>
        /// <param name="alpha"></param>
        /// <param name="logger"></param>
        public KLookup(IKRouter<TKNodeId> router, IKEndpointInvoker<TKNodeId> invoker, int alpha = 3, ILogger logger = null)
        {
            this.router = router ?? throw new ArgumentNullException(nameof(router));
            this.invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
            this.alpha = alpha;
            this.logger = logger;
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
            var init = await router.GetNextHopAsync(key, alpha, cancellationToken);

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
                        if (n.Id.Equals(router.SelfId) == false)
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
                                if (i.Id.Equals(router.SelfId) == false)
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

    }

}
