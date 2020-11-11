using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Cogito.Memory;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Alethic.Kademlia
{

    /// <summary>
    /// Implements a fixed Kademlia routing table.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public class KFixedTableRouter<TNodeId> : KFixedTableRouter, IKRouter<TNodeId>
        where TNodeId : unmanaged
    {

        readonly IOptions<KFixedTableRouterOptions> options;
        readonly IKHost<TNodeId> host;
        readonly ILogger logger;

        readonly KBucket<TNodeId>[] buckets;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="host"></param>
        /// <param name="invoker"></param>
        /// <param name="logger"></param>
        public KFixedTableRouter(IOptions<KFixedTableRouterOptions> options, IKHost<TNodeId> host, IKInvoker<TNodeId> invoker, ILogger logger)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.host = host ?? throw new ArgumentNullException(nameof(host));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            logger.LogInformation("Initializing Fixed Table Router with {NodeId}.", host.SelfId);
            buckets = new KBucket<TNodeId>[Unsafe.SizeOf<TNodeId>() * 8];
            for (var i = 0; i < buckets.Length; i++)
                buckets[i] = new KBucket<TNodeId>(host, invoker, logger, options.Value.K);
        }

        /// <summary>
        /// Gets the fixed size of the routing table buckets.
        /// </summary>
        public int K => options.Value.K;

        /// <summary>
        /// Gets the bucket associated with the specified node ID.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        internal KBucket<TNodeId> GetBucket(in TNodeId node)
        {
            var i = GetBucketIndex(host.SelfId, node);
            logger.LogTrace("Bucket lookup for {NodeId} returned {BucketIndex}.", node, i);
            return buckets[i];
        }

#if NETSTANDARD2_1

        /// <summary>
        /// Gets the <paramref name="k"/> closest peers to the specified node ID.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="k"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public IAsyncEnumerable<KNodeEndpointInfo<TNodeId>> SelectAsync(in TNodeId key, int k = 0, CancellationToken cancellationToken = default)
        {
            return SelectAsync(key, k, cancellationToken);
        }

        /// <summary>
        /// Gets the <paramref name="k"/> closest peers to the specified node ID.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="k"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async IAsyncEnumerable<KNodeEndpointInfo<TNodeId>> SelectAsync(TNodeId key, int k = 0, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var i in await SelectAsyncIter(key, k, cancellationToken))
                yield return i;
        }

#else

        /// <summary>
        /// Gets the <paramref name="k"/> closest peers to the specified node ID.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="k"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<IEnumerable<KNodeEndpointInfo<TNodeId>>> SelectAsync(in TNodeId key, int k = 0, CancellationToken cancellationToken = default)
        {
            return SelectAsyncIter(key, k, cancellationToken);
        }

#endif

        /// <summary>
        /// Gets the <paramref name="k"/> closest peers to the specified node ID.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="k"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<IEnumerable<KNodeEndpointInfo<TNodeId>>> SelectAsyncIter(in TNodeId key, int k, CancellationToken cancellationToken = default)
        {
            if (k == 0)
                return GetBucket(key).SelectAsync(key, cancellationToken);

            // take first bucket; then append others; pretty inefficient
            var c = new KNodeIdDistanceComparer<TNodeId>(key);
            var f = key.Equals(host.SelfId) ? null : buckets[GetBucketIndex(host.SelfId, key)];
            var s = f == null ? Enumerable.Empty<KBucket<TNodeId>>() : new[] { f };
            var l = s.Concat(buckets.Except(s)).SelectMany(i => i).OrderBy(i => i.NodeId, c).Take(k).Select(i => new KNodeEndpointInfo<TNodeId>(i.NodeId, i.Endpoints));
            return new ValueTask<IEnumerable<KNodeEndpointInfo<TNodeId>>>(l);
        }

        /// <summary>
        /// Updates the endpoints for the peer within the table.
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="peer"></param>
        /// <param name="endpoints"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask UpdateAsync(in TNodeId peer, IEnumerable<IKProtocolEndpoint<TNodeId>> endpoints, CancellationToken cancellationToken = default)
        {
            if (peer.Equals(host.SelfId))
            {
                logger.LogError("Peer update request for self. Discarding.");
                return new ValueTask(Task.CompletedTask);
            }

            return GetBucket(peer).UpdateAsync(peer, endpoints, cancellationToken);
        }

        /// <summary>
        /// Gets the number of peers known by the table.
        /// </summary>
        public int Count => buckets.Sum(i => i.Count);

#if NETSTANDARD2_1

        /// <summary>
        /// Iterates all of the known peers.
        /// </summary>
        /// <returns></returns>
        public IAsyncEnumerator<KNodeEndpointInfo<TNodeId>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return buckets.SelectMany(i => i).Select(i => new KNodeEndpointInfo<TNodeId>(i.NodeId, i.Endpoints)).ToAsyncEnumerable().GetAsyncEnumerator();
        }

#endif

        /// <summary>
        /// Iterates all of the known peers.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KNodeEndpointInfo<TNodeId>> GetEnumerator()
        {
            return buckets.SelectMany(i => i).Select(i => new KNodeEndpointInfo<TNodeId>(i.NodeId, i.Endpoints)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }

    /// <summary>
    /// Describes a Kademlia routing table.
    /// </summary>
    public abstract class KFixedTableRouter
    {

        /// <summary>
        /// Calculates the bucket index that should be used for the <paramref name="other"/> node in a table owned by <paramref name="self"/>.
        /// </summary>
        /// <typeparam name="TNodeId"></typeparam>
        /// <param name="self"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        internal static int GetBucketIndex<TNodeId>(in TNodeId self, in TNodeId other)
            where TNodeId : unmanaged
        {
            if (self.Equals(other))
                throw new ArgumentException("Cannot get bucket for own node.");

            // calculate distance between nodes
            var o = (Span<byte>)stackalloc byte[KNodeId<TNodeId>.SizeOf];
            KNodeId<TNodeId>.CalculateDistance(self, other, o);

            // leading zeros is our bucket position
            var z = ((ReadOnlySpan<byte>)o).CountLeadingZeros();
            return o.Length * 8 - z - 1;
        }

    }

}
