using System;
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
    public class KFixedRoutingTable<TKNodeId, TKPeerData> : KFixedRoutingTable, IKRouter<TKNodeId, TKPeerData>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
        where TKPeerData : IKEndpointProvider<TKNodeId>
    {

        readonly TKNodeId selfId;
        readonly TKPeerData selfData;
        readonly int k;
        readonly KBucket<TKNodeId, TKPeerData>[] buckets;

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
        /// Updates the reference to the node within the table.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="peerData"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask TouchAsync(in TKNodeId nodeId, in TKPeerData peerData, CancellationToken cancellationToken = default)
        {
            return GetBucket(nodeId).TouchAsync(nodeId, peerData, cancellationToken);
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
            KNodeId.CalculateDistance(self, other, o);

            // leading zeros is our bucket position
            var z = ((ReadOnlySpan<byte>)o).CountLeadingZeros();
            return o.Length * 8 - z - 1;
        }

    }

}
