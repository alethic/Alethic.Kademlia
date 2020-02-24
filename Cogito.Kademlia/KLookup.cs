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

        /// <summary>
        /// Describes a result from one of the Find* methods.
        /// </summary>
        readonly struct FindResult
        {

            readonly IEnumerable<KPeerEndpointInfo<TKNodeId>> peers;
            readonly ReadOnlyMemory<byte>? value;

            /// <summary>
            /// Initializes a new instance.
            /// </summary>
            /// <param name="peers"></param>
            /// <param name="value"></param>
            public FindResult(IEnumerable<KPeerEndpointInfo<TKNodeId>> peers, ReadOnlyMemory<byte>? value)
            {
                this.peers = peers;
                this.value = value;
            }

            /// <summary>
            /// Gets the set of peers returned from the find method.
            /// </summary>
            public IEnumerable<KPeerEndpointInfo<TKNodeId>> Peers => peers;

            /// <summary>
            /// Optionally gets the value returned from the find method.
            /// </summary>
            public ReadOnlyMemory<byte>? Value => value;

        }

        /// <summary>
        /// Describes a version of the find function.
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        delegate ValueTask<FindResult> FindFunc(KPeerEndpointInfo<TKNodeId> peer, TKNodeId key, CancellationToken cancellationToken);

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
            return LookupAsync(key, FindNodeAsync, cancellationToken);
        }

        /// <summary>
        /// Initiates a lookup for the specified key, returning the closest discovered node.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KLookupResult<TKNodeId>> LookupValueAsync(in TKNodeId key, CancellationToken cancellationToken = default)
        {
            return LookupAsync(key, FindValueAsync, cancellationToken);
        }

        /// <summary>
        /// Begins a search process for the specified node.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KLookupResult<TKNodeId>> LookupAsync(TKNodeId key, FindFunc func, CancellationToken cancellationToken = default)
        {
            if (func is null)
                throw new ArgumentNullException(nameof(func));

            var wait = new HashSet<Task<FindResult>>();
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

                    // schedule queries of our closest nodes
                    while (wait.Count < alpha && todo.Count > 0)
                    {
                        // schedule new node to query
                        var peer = todo.DeleteMin();
                        if (peer.Id.Equals(router.SelfId) == false)
                            wait.Add(func(peer, key, stop.Token).AsTask().ContinueWith((r, o) => r.Result, peer));
                    }

                    // we have at least one task in the task pool to wait for
                    if (wait.Count > 0)
                    {
                        // wait for first finished task
                        var find = await Task.WhenAny(wait);
                        wait.Remove(find);

                        // skip cancelled tasks
                        if (find.IsCanceled)
                            continue;

                        // skip failed tasks
                        if (find.Exception != null)
                        {
                            // ignore timeouts
                            if (find.Exception.InnerException is TimeoutException)
                                continue;

                            logger?.LogError(find.Exception, "Received error from lookup task.");
                            continue;
                        }

                        // task returned something; must have succeeded in our lookup
                        if (find.Result.Peers != null)
                        {
                            // extract the peer this request was destined to
                            var peer = (KPeerEndpointInfo<TKNodeId>)find.AsyncState;

                            // method returned the value; we can stop looking and return the value and our path
                            if (find.Result.Value != null)
                                return new KLookupResult<TKNodeId>(key, path, peer, find.Result.Value);

                            // after we've received a successful result
                            // mark the node as one we've encountered which did not return a value
                            path.Add(peer);

                            // path should only contain top K nodes
                            while (path.Count > router.K)
                                path.DeleteMax();

                            // iterate over newly retrieved peers
                            foreach (var i in find.Result.Peers)
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
            return new KLookupResult<TKNodeId>(key, path, null);
        }

        /// <summary>
        /// Issues a FIND_NODE request to the peer, looking for the specified key, and returns the resolved peers
        /// and their endpoints.
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<FindResult> FindNodeAsync(KPeerEndpointInfo<TKNodeId> peer, TKNodeId key, CancellationToken cancellationToken)
        {
            var r = await invoker.FindNodeAsync(peer.Endpoints, key, cancellationToken);
            if (r.Status == KResponseStatus.Success)
                return new FindResult(r.Body.Peers, null);

            return new FindResult(null, null);
        }

        /// <summary>
        /// Issues a FIND_VALUE request to the peer, looking for the specified key, and returns the resolved peers
        /// and their endpoints, and optionally a value if the value exists.
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<FindResult> FindValueAsync(KPeerEndpointInfo<TKNodeId> peer, TKNodeId key, CancellationToken cancellationToken)
        {
            var r = await invoker.FindValueAsync(peer.Endpoints, key, cancellationToken);
            if (r.Status == KResponseStatus.Success)
                return new FindResult(r.Body.Peers, r.Body.Value);

            return new FindResult(null, null);
        }

    }

}
