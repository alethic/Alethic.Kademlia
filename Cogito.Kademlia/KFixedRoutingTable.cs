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
    /// <typeparam name="TKPeerData"></typeparam>
    public class KFixedRoutingTable<TKNodeId, TKPeerData> : KTable, IKRoutingTable<TKNodeId, TKPeerData>
        where TKNodeId : struct, IKNodeId<TKNodeId>
    {

        readonly TKNodeId self;
        readonly IKProtocol<TKNodeId, TKPeerData> protocol;
        readonly int k;
        readonly KBucket<TKNodeId, TKPeerData>[] buckets;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="protocol"></param>
        /// <param name="k"></param>
        public KFixedRoutingTable(TKNodeId self, IKProtocol<TKNodeId, TKPeerData> protocol, int k = 20)
        {
            this.self = self;
            this.protocol = protocol;
            this.k = k;

            if (self.Size % 8 != 0)
                throw new ArgumentException("NodeId must have a distance size which is a multiple of 8.");

            buckets = new KBucket<TKNodeId, TKPeerData>[self.Size];
            for (var i = 0; i < buckets.Length; i++)
                buckets[i] = new KBucket<TKNodeId, TKPeerData>(k);
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
        internal KBucket<TKNodeId, TKPeerData> GetBucket(TKNodeId nodeId)
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
        public ValueTask TouchAsync(TKNodeId nodeId, TKPeerData nodeData = default, IKPeerEvents nodeEvents = null, CancellationToken cancellationToken = default)
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
        /// Calculates the bucket index that should be used for the <paramref name="r"/> node in a table owned by <paramref name="l"/>.
        /// </summary>
        /// <typeparam name="TKNodeId"></typeparam>
        /// <param name="l"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        internal static int GetBucketIndex<TKNodeId>(TKNodeId l, TKNodeId r)
            where TKNodeId : struct, IKNodeId<TKNodeId>
        {
            if (l.Equals(r))
                throw new ArgumentException("Cannot get bucket for own node.");

            // calculate distance between nodes
            var o = (Span<byte>)stackalloc byte[l.Size / 8];
            KNodeIdExtensions.CalculateDistance(l, r, o);

            // leading zeros is our bucket position
            var z = ((ReadOnlySpan<byte>)o).CountLeadingZeros();
            return r.Size - z - 1;
        }

    }

}
