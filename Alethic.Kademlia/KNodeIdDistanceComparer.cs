using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Alethic.Kademlia
{

    /// <summary>
    /// Implements a distance comparison between an established node.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public readonly struct KNodeIdDistanceComparer<TNodeId> : IComparer<TNodeId>
        where TNodeId : unmanaged
    {

        readonly TNodeId n;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="n"></param>
        public KNodeIdDistanceComparer(in TNodeId n)
        {
            this.n = n;
        }

        public int Compare(TNodeId x, TNodeId y)
        {
            // two equal nodes must be same distance from target
            if (x.Equals(y))
                return 0;

            // distance from target to xss
            var a = (Span<byte>)stackalloc byte[Unsafe.SizeOf<TNodeId>()];
            KNodeId<TNodeId>.CalculateDistance(n, x, a);

            // distance from target to y
            var b = (Span<byte>)stackalloc byte[Unsafe.SizeOf<TNodeId>()];
            KNodeId<TNodeId>.CalculateDistance(n, y, b);

            // who is closest?
            return a.SequenceCompareTo(b);
        }

    }

}
