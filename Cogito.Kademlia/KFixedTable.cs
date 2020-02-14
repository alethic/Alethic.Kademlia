using System;
using System.Threading;
using System.Threading.Tasks;

using Cogito.Kademlia.Core;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Implements a fixed Kademlia routing table.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    /// <typeparam name="TKNodeData"></typeparam>
    public class KFixedTable<TKNodeId, TKNodeData> : KTable, IKTable<TKNodeId, TKNodeData>
        where TKNodeId : struct, IKNodeId<TKNodeId>
    {

        readonly TKNodeId self;
        readonly IKNodeProtocol<TKNodeId, TKNodeData> protocol;
        readonly int k;
        readonly KBucket<TKNodeId, TKNodeData>[] buckets;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="protocol"></param>
        /// <param name="k"></param>
        public KFixedTable(TKNodeId self, IKNodeProtocol<TKNodeId, TKNodeData> protocol, int k = 20)
        {
            this.self = self;
            this.protocol = protocol;
            this.k = k;

            if (self.DistanceSize % 8 != 0)
                throw new ArgumentException("NodeId must have a distance size which is a multiple of 8.");

            buckets = new KBucket<TKNodeId, TKNodeData>[self.DistanceSize];
            for (var i = 0; i < buckets.Length; i++)
                buckets[i] = new KBucket<TKNodeId, TKNodeData>(k);
        }

        /// <summary>
        /// Gets the fixed size of the routing table buckets.
        /// </summary>
        public int K => k;

        /// <summary>
        /// Gets the bucket associated with the specified node ID.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        internal KBucket<TKNodeId, TKNodeData> GetBucket(TKNodeId nodeId)
        {
            return buckets[GetBucketIndex(self, nodeId)];
        }

        /// <summary>
        /// Updates the reference to the node within the table.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="nodeData"></param>
        /// <param name="nodeEvents"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask TouchAsync(TKNodeId nodeId, TKNodeData nodeData = default, IKNodeEvents nodeEvents = null, CancellationToken cancellationToken = default)
        {
            return GetBucket(nodeId).TouchAsync(nodeId, nodeData, nodeEvents, protocol, cancellationToken);
        }

    }

    /// <summary>
    /// Describes a Kademlia routing table.
    /// </summary>
    public abstract class KTable
    {

        /// <summary>
        /// Calculates the bucket index that should be used for the <paramref name="other"/> node in a table owned by <paramref name="self"/>.
        /// </summary>
        /// <typeparam name="TKNodeId"></typeparam>
        /// <param name="self"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        internal static int GetBucketIndex<TKNodeId>(TKNodeId self, TKNodeId other)
            where TKNodeId : IKNodeId<TKNodeId>
        {
            if (self.Equals(other))
                throw new ArgumentException("Cannot get bucket for own node.");

            var s = self.DistanceSize / 8;
            var d = (Span<byte>)stackalloc byte[s];
            self.CalculateDistance(other, d);
            var z = ((ReadOnlySpan<byte>)d).CountLeadingZeros();
            return other.DistanceSize - z - 1;
        }

    }

}
