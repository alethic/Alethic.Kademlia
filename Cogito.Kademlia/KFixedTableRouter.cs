using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Cogito.Kademlia.Core;
using Cogito.Linq;

using Microsoft.Extensions.Logging;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Implements a fixed Kademlia routing table with the default peer data type.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public class KFixedTableRouter<TKNodeId> : KFixedTableRouter<TKNodeId, KPeerData<TKNodeId>>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="selfId"></param>
        /// <param name="invoker"></param>
        /// <param name="k"></param>
        /// <param name="logger"></param>
        public KFixedTableRouter(in TKNodeId selfId, IKEndpointInvoker<TKNodeId> invoker, int k = DefaultKSize, ILogger logger = null) :
            base(selfId, new KPeerData<TKNodeId>(), invoker, k, logger)
        {

        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="selfId"></param>
        /// <param name="selfData"></param>
        /// <param name="invoker"></param>
        /// <param name="k"></param>
        /// <param name="logger"></param>
        public KFixedTableRouter(in TKNodeId selfId, in KPeerData<TKNodeId> selfData, IKEndpointInvoker<TKNodeId> invoker, int k = DefaultKSize, ILogger logger = null) :
            base(selfId, selfData, invoker, k, logger)
        {

        }

    }

    /// <summary>
    /// Implements a fixed Kademlia routing table.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    /// <typeparam name="TKPeerData"></typeparam>
    public class KFixedTableRouter<TKNodeId, TKPeerData> : KFixedTableRouter, IKRouter<TKNodeId, TKPeerData>, IEnumerable<KeyValuePair<TKNodeId, TKPeerData>>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
        where TKPeerData : IKEndpointProvider<TKNodeId>, new()
    {

        public const int DefaultKSize = 20;

        readonly TKNodeId selfId;
        readonly TKPeerData selfData;
        readonly IKEndpointInvoker<TKNodeId> invoker;
        readonly int k;
        readonly ILogger logger;
        readonly KBucket<TKNodeId, TKPeerData>[] buckets;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="selfId"></param>
        /// <param name="selfData"></param>
        /// <param name="invoker"></param>
        /// <param name="k"></param>
        /// <param name="logger"></param>
        public KFixedTableRouter(in TKNodeId selfId, in TKPeerData selfData, IKEndpointInvoker<TKNodeId> invoker, int k = DefaultKSize, ILogger logger = null)
        {
            if (k < 1)
                throw new ArgumentOutOfRangeException("The value of k must be greater than or equal to 1.");

            this.selfId = selfId;
            this.selfData = selfData;
            this.invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
            this.k = k;
            this.logger = logger;

            logger?.LogInformation("Initializing Fixed Table Router with {NodeId}.", selfId);
            buckets = new KBucket<TKNodeId, TKPeerData>[Unsafe.SizeOf<TKNodeId>() * 8];
            for (var i = 0; i < buckets.Length; i++)
                buckets[i] = new KBucket<TKNodeId, TKPeerData>(k, invoker, logger);
        }

        /// <summary>
        /// Gets the ID of the node itself.
        /// </summary>
        public TKNodeId SelfId => selfId;

        /// <summary>
        /// Gets the data of the node itself.
        /// </summary>
        public TKPeerData SelfData => selfData;

        /// <summary>
        /// Gets the fixed size of the routing table buckets.
        /// </summary>
        public int K => k;

        /// <summary>
        /// Gets the bucket associated with the specified node ID.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        internal KBucket<TKNodeId, TKPeerData> GetBucket(in TKNodeId nodeId)
        {
            var i = GetBucketIndex(selfId, nodeId);
            logger?.LogTrace("Bucket lookup for {NodeId} returned {BucketIndex}.", nodeId, i);
            return buckets[i];
        }

        /// <summary>
        /// Gets the data for the peer within the table.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<TKPeerData> GetPeerDataAsync(in TKNodeId nodeId, CancellationToken cancellationToken)
        {
            return GetBucket(nodeId).GetPeerDataAsync(nodeId, cancellationToken);
        }

        /// <summary>
        /// Gets the <paramref name="k"/> closest peers to the specified node ID.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="k"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<IEnumerable<KPeerEndpointInfo<TKNodeId>>> GetNextHopAsync(in TKNodeId key, int k, CancellationToken cancellationToken = default)
        {
            logger?.LogTrace("Obtaining top {k} peers for {Key}.", k, key);

            // take first bucket; then append others; pretty inefficient
            var c = new KNodeIdDistanceComparer<TKNodeId>(key);
            var l = Enumerable.Empty<KBucket<TKNodeId, TKPeerData>>().Append(buckets[GetBucketIndex(selfId, key)]).Concat(buckets).Distinct().SelectMany(i => i).OrderBy(i => i.Id, c).Take(k).Select(i => new KPeerEndpointInfo<TKNodeId>(i.Id, i.Data.Endpoints.ToArray())).ToArray();
            return new ValueTask<IEnumerable<KPeerEndpointInfo<TKNodeId>>>(l);
        }

        /// <summary>
        /// Updates the endpoints for the peer within the table.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="endpoint"></param>
        /// <param name="additional"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask UpdatePeerAsync(in TKNodeId nodeId, IKEndpoint<TKNodeId> endpoint, IEnumerable<IKEndpoint<TKNodeId>> additional, CancellationToken cancellationToken = default)
        {
            logger?.LogTrace("Received request to update peer {NodeId} with {Endpoint}.", nodeId, endpoint);

            if (nodeId.Equals(SelfId))
            {
                logger?.LogError("Peer update request for self at {Endpoint}. Discarding.", endpoint);
                return new ValueTask(Task.CompletedTask);
            }

            return GetBucket(nodeId).UpdatePeerAsync(nodeId, endpoint, additional, cancellationToken);
        }

        /// <summary>
        /// Gets the number of peers known by the table.
        /// </summary>
        public int Count => buckets.Sum(i => i.Count);

        /// <summary>
        /// Iterates all of the known peers.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<TKNodeId, TKPeerData>> GetEnumerator()
        {
            return buckets.SelectMany(i => i).Select(i => new KeyValuePair<TKNodeId, TKPeerData>(i.Id, i.Data)).GetEnumerator();
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
        /// <typeparam name="TKNodeId"></typeparam>
        /// <param name="self"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        internal static int GetBucketIndex<TKNodeId>(in TKNodeId self, in TKNodeId other)
            where TKNodeId : unmanaged, IKNodeId<TKNodeId>
        {
            if (self.Equals(other))
                throw new ArgumentException("Cannot get bucket for own node.");

            // calculate distance between nodes
            var o = (Span<byte>)stackalloc byte[KNodeId<TKNodeId>.SizeOf];
            KNodeId<TKNodeId>.CalculateDistance(self, other, o);

            // leading zeros is our bucket position
            var z = ((ReadOnlySpan<byte>)o).CountLeadingZeros();
            return o.Length * 8 - z - 1;
        }

    }

}
