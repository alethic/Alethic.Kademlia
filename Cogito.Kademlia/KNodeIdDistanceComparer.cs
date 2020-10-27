using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Implements a distance comparison between an established node.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KNodeIdDistanceComparer<TKNodeId> : IComparer<TKNodeId>
        where TKNodeId : unmanaged
    {

        readonly TKNodeId n;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="n"></param>
        public KNodeIdDistanceComparer(in TKNodeId n)
        {
            this.n = n;
        }

        public int Compare(TKNodeId x, TKNodeId y)
        {
            // two equal nodes must be same distance from target
            if (x.Equals(y))
                return 0;

            // distance from target to xss
            var a = (Span<byte>)stackalloc byte[Unsafe.SizeOf<TKNodeId>()];
            KNodeId<TKNodeId>.CalculateDistance(n, x, a);

            // distance from target to y
            var b = (Span<byte>)stackalloc byte[Unsafe.SizeOf<TKNodeId>()];
            KNodeId<TKNodeId>.CalculateDistance(n, y, b);

            // who is closest?
            return a.SequenceCompareTo(b);
        }

    }

}
