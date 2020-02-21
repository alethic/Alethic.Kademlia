using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Cogito.Kademlia.Core;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Implements a fixed Kademlia routing table with the default peer data type.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public class KFixedRoutingTable<TKNodeId> : KFixedRoutingTable<TKNodeId, KPeerData<TKNodeId>>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="selfId"></param>
        /// <param name="selfData"></param>
        /// <param name="k"></param>
        public KFixedRoutingTable(in TKNodeId selfId, int k = 20) :
            base(selfId, new KPeerData<TKNodeId>(), k)
        {

        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="selfId"></param>
        /// <param name="selfData"></param>
        /// <param name="k"></param>
        public KFixedRoutingTable(in TKNodeId selfId, in KPeerData<TKNodeId> selfData, int k = 20) :
            base(selfId, selfData, k)
        {

        }

    }

    /// <summary>
    /// Implements a fixed Kademlia routing table.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    /// <typeparam name="TKPeerData"></typeparam>
    public class KFixedRoutingTable<TKNodeId, TKPeerData> : KFixedRoutingTable, IKRouter<TKNodeId, TKPeerData>, IEnumerable<KeyValuePair<TKNodeId, TKPeerData>>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
        where TKPeerData : IKEndpointProvider<TKNodeId>, new()
    {

        readonly TKNodeId selfId;
        readonly TKPeerData selfData;
        readonly int k;
        readonly KBucket<TKNodeId, TKPeerData>[] buckets;

        IKEngine<TKNodeId> engine;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="selfId"></param>
        /// <param name="k"></param>
        public KFixedRoutingTable(in TKNodeId selfId, in TKPeerData selfData, int k = 20)
        {
            this.selfId = selfId;
            this.selfData = selfData;
            this.k = k;

            buckets = new KBucket<TKNodeId, TKPeerData>[Unsafe.SizeOf<TKNodeId>() * 8];
            for (var i = 0; i < buckets.Length; i++)
                buckets[i] = new KBucket<TKNodeId, TKPeerData>(k);
        }

        /// <summary>
        /// Attaches the routing table to the given engine.
        /// </summary>
        /// <param name="engine"></param>
        void IKRouter<TKNodeId>.Attach(IKEngine<TKNodeId> engine)
        {
            if (engine == null)
                throw new ArgumentNullException(nameof(engine));
            if (this.engine != null && this.engine != engine)
                throw new InvalidOperationException();

            this.engine = engine;
        }

        /// <summary>
        /// Gets the currently associated engine.
        /// </summary>
        public IKEngine<TKNodeId> Engine => engine;

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
            return buckets[i];
        }

        /// <summary>
        /// Gets the data for the peer within the table.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<TKPeerData> GetPeerAsync(in TKNodeId nodeId, CancellationToken cancellationToken)
        {
            return GetBucket(nodeId).GetPeerAsync(nodeId, cancellationToken);
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
            if (nodeId.Equals(SelfId))
                return new ValueTask(Task.CompletedTask);
            else
                return GetBucket(nodeId).UpdatePeerAsync(nodeId, endpoint, additional, cancellationToken);
        }

        /// <summary>
        /// Gets the <paramref name="k"/> closest peers to the specified node ID.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="k"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<IEnumerable<KPeerEndpointInfo<TKNodeId>>> GetPeersAsync(in TKNodeId key, int k, CancellationToken cancellationToken = default)
        {
            var c = new KNodeIdDistanceComparer<TKNodeId>(key);
            var l = buckets.SelectMany(i => i).OrderBy(i => i.Id, c).Take(k).Select(i => new KPeerEndpointInfo<TKNodeId>(i.Id, i.Data.Endpoints.ToArray())).ToArray();
            return new ValueTask<IEnumerable<KPeerEndpointInfo<TKNodeId>>>(l);
        }

        /// <summary>
        /// Initiates a refresh of the routing table buckets.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask RefreshAsync(CancellationToken cancellationToken = default)
        {
            return new ValueTask(Task.WhenAll(Enumerable.Range(1, buckets.Length - 1).Select(i => GetRandomNodeIdFromBucket(i)).Select(i => engine.LookupAsync(i, cancellationToken).AsTask())));
        }

        /// <summary>
        /// Generates a random node ID with the specified number of right masked bits set.
        /// </summary>
        /// <param name="bucket"></param>
        /// <returns></returns>
        TKNodeId GetRandomNodeIdFromBucket(int bucket)
        {
            // set all except suffix
            var selfMask = new BitArray(KNodeId<TKNodeId>.SizeOf() * 8);
            for (int i = 0; i < selfMask.Length - bucket; i++)
                selfMask.Set(i, true);

            // set only suffix
            var randMask = new BitArray(selfMask).Not();

            var selfNode = (Span<byte>)stackalloc byte[KNodeId<TKNodeId>.SizeOf()];
            SelfId.Write(selfNode);
            var selfBuff = new BitArray(selfNode.ToArray());
            selfBuff.And(selfMask);

            var randNode = (Span<byte>)stackalloc byte[KNodeId<TKNodeId>.SizeOf()];
            KNodeId<TKNodeId>.Create().Write(randNode);
            var randBuff = new BitArray(randNode.ToArray());
            randBuff.And(randMask);

            var cmplBuff = selfBuff.Or(randBuff);
            var cmplNode = new byte[KNodeId<TKNodeId>.SizeOf()];
            cmplBuff.CopyTo(cmplNode, 0);
            var cmplFins = KNodeId<TKNodeId>.Read(cmplNode);

            return cmplFins;
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
    public abstract class KFixedRoutingTable
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
            var o = (Span<byte>)stackalloc byte[Unsafe.SizeOf<TKNodeId>()];
            KNodeId<TKNodeId>.CalculateDistance(self, other, o);

            // leading zeros is our bucket position
            var z = ((ReadOnlySpan<byte>)o).CountLeadingZeros();
            return o.Length * 8 - z - 1;
        }

    }

}
