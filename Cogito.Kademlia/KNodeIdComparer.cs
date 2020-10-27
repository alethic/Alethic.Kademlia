using System;
using System.Collections.Generic;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Implements a distance comparison between an established node.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KNodeIdComparer<TKNodeId> : IComparer<TKNodeId>
        where TKNodeId : unmanaged
    {

        /// <summary>
        /// Gets the default comparer instance.
        /// </summary>
        public static KNodeIdComparer<TKNodeId> Default => new KNodeIdComparer<TKNodeId>();

        public int Compare(TKNodeId x, TKNodeId y)
        {
            // two equal nodes must be same distance from target
            if (x.Equals(y))
                return 0;

            var a = (Span<byte>)stackalloc byte[KNodeId<TKNodeId>.SizeOf];
            x.Write(a);

            var b = (Span<byte>)stackalloc byte[KNodeId<TKNodeId>.SizeOf];
            y.Write(b);

            // who is closest?
            return a.SequenceCompareTo(b);
        }

    }

}
