using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Cogito.Collections;

using Microsoft.Extensions.Logging;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Provides the node lookup operation logic against a <see cref="IKRouter{TKNodeId}"/>.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public class KLookup<TKNodeId> : IKLookup<TKNodeId>
        where TKNodeId : unmanaged
    {

        /// <summary>
        /// Describes a result from one of the Find* methods.
        /// </summary>
        readonly struct FindResult
        {

            readonly IEnumerable<KPeerEndpointInfo<TKNodeId>> peers;
            readonly KValueInfo? value;

            /// <summary>
            /// Initializes a new instance.
            /// </summary>
            /// <param name="peers"></param>
            /// <param name="value"></param>
            public FindResult(IEnumerable<KPeerEndpointInfo<TKNodeId>> peers, in KValueInfo? value)
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
            public KValueInfo? Value => value;

        }

        /// <summary>
        /// Describes a result from the Lookup method.
        /// </summary>
        readonly struct LookupResult
        {

            readonly TKNodeId key;
            readonly IEnumerable<KPeerEndpointInfo<TKNodeId>> peers;
            readonly KPeerEndpointInfo<TKNodeId>? source;
            readonly KValueInfo? value;

            /// <summary>
            /// Initializes a new instance.
            /// </summary>
            /// <param name="key"></param>
            /// <param name="peers"></param>
            /// <param name="source"></param>
            /// <param name="value"></param>
            public LookupResult(in TKNodeId key, IEnumerable<KPeerEndpointInfo<TKNodeId>> peers, in KPeerEndpointInfo<TKNodeId>? source, in KValueInfo? value)
            {
                this.key = key;
                this.peers = peers;
                this.source = source;
                this.value = value;
            }

            /// <summary>
            /// Gets the original search key.
            /// </summary>
            public TKNodeId Key => key;

            /// <summary>
            /// Gets the set of peers returned from the find method.
            /// </summary>
            public IEnumerable<KPeerEndpointInfo<TKNodeId>> Peers => peers;

            /// <summary>
            /// Gets the final node that returned the result.
            /// </summary>
            public KPeerEndpointInfo<TKNodeId>? Source => source;

            /// <summary>
            /// Optionally gets the value returned from the find method.
            /// </summary>
            public KValueInfo? Value => value;

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
        readonly int cache;
        readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="router"></param>
        /// <param name="invoker"></param>
        /// <param name="alpha">Number of outstanding FIND_ RPC requests to keep in flight.</param>
        /// <param name="cache">Number of nodes to cache resulting values at.</param>
        /// <param name="logger"></param>
        public KLookup(IKRouter<TKNodeId> router, IKEndpointInvoker<TKNodeId> invoker, int alpha = 3, int cache = 1, ILogger logger = null)
        {
            this.router = router ?? throw new ArgumentNullException(nameof(router));
            this.invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
            this.alpha = alpha;
            this.cache = cache;
            this.logger = logger;
        }

        /// <summary>
        /// Initiates a lookup for the specified key, returning the closest discovered node.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KLookupNodeResult<TKNodeId>> LookupNodeAsync(in TKNodeId key, CancellationToken cancellationToken = default)
        {
            return LookupNodeAsync(key, cancellationToken);
        }

        async ValueTask<KLookupNodeResult<TKNodeId>> LookupNodeAsync(TKNodeId key, CancellationToken cancellationToken = default)
        {
            var r = await LookupAsync(key, FindNodeAsync, cancellationToken);
            return new KLookupNodeResult<TKNodeId>(r.Key, r.Peers);
        }

        /// <summary>
        /// Initiates a lookup for the specified key, returning the closest discovered node.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KLookupValueResult<TKNodeId>> LookupValueAsync(in TKNodeId key, CancellationToken cancellationToken = default)
        {
            return LookupValueAsync(key, cancellationToken);
        }

        async ValueTask<KLookupValueResult<TKNodeId>> LookupValueAsync(TKNodeId key, CancellationToken cancellationToken = default)
        {
            var r = await LookupAsync(key, FindValueAsync, cancellationToken);

            // value was returned, store at first path member
            if (r.Value is KValueInfo value)
                await CacheAsync(r.Peers.Take(cache), key, value, cancellationToken);

            return new KLookupValueResult<TKNodeId>(r.Key, r.Peers, r.Source, r.Value);
        }

        /// <summary>
        /// Stores the key value at the specified set of peers to function as a cache.
        /// </summary>
        /// <param name="peers"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task CacheAsync(IEnumerable<KPeerEndpointInfo<TKNodeId>> peers, TKNodeId key, KValueInfo value, CancellationToken cancellationToken)
        {
            return Task.WhenAll(peers.Select(i => invoker.StoreAsync(i.Endpoints, key, KStoreRequestMode.Replica, value, cancellationToken).AsTask()));
        }

        /// <summary>
        /// Begins a search process for the specified node.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<LookupResult> LookupAsync(TKNodeId key, FindFunc func, CancellationToken cancellationToken = default)
        {
            if (func is null)
                throw new ArgumentNullException(nameof(func));

            var wait = new HashSet<Task<FindResult>>();
            var comp = new KNodeIdDistanceComparer<TKNodeId>(key);

            // kill is used to cancel outstanding tasks early
            var kill = new CancellationTokenSource();
            var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, kill.Token);

            // find our own closest peers to seed from
            var init = await router.SelectPeersAsync(key, alpha, cancellationToken);

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
                        if (peer.Id.Equals(router.Self) == false)
                            wait.Add(func(peer, key, stop.Token).AsTask().ContinueWith((r, o) => r.Result, peer, TaskContinuationOptions.OnlyOnRanToCompletion));
                    }

                    // we have at least one task in the task pool to wait for
                    if (wait.Count > 0)
                    {
                        // wait for first finished task
                        var find = await TaskWhenAny(wait);
                        wait.Remove(find);

                        // skip cancelled tasks
                        if (find.IsCanceled)
                            continue;

                        // skip failed tasks
                        if (find.Exception != null)
                        {
                            // ignore various cancellation exceptions
                            if (find.Exception.InnerException is TimeoutException)
                                continue;
                            if (find.Exception.InnerException is OperationCanceledException)
                                continue;

                            logger?.LogError(find.Exception, "Received error from lookup task.");
                            continue;
                        }

                        // extract the peer this request was destined to
                        var peer = (KPeerEndpointInfo<TKNodeId>)find.AsyncState;

                        // method returned the value; we can stop looking and return the value and our path
                        if (find.Result.Value != null)
                            return new LookupResult(key, path, peer, find.Result.Value);

                        // task returned more peers, lets begin working on them
                        if (find.Result.Peers != null)
                        {
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
                                if (i.Id.Equals(router.Self) == false)
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
            return new LookupResult(key, path, null, null);
        }

        /// <summary>
        /// Waits for any of the tasks to complete, but traps cancellation exceptions.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="tasks"></param>
        /// <returns></returns>
        async Task<Task<TResult>> TaskWhenAny<TResult>(IEnumerable<Task<TResult>> tasks)
        {
            try
            {
                return await Task.WhenAny(tasks);
            }
            catch (TaskCanceledException e)
            {
                return (Task<TResult>)e.Task;
            }
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
