using System;
using System.Collections.Generic;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Implements a distance comparison between an established node.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public readonly struct KNodeIdComparer<TNodeId> : IComparer<TNodeId>
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Gets the default comparer instance.
        /// </summary>
        public static KNodeIdComparer<TNodeId> Default => new KNodeIdComparer<TNodeId>();

        public int Compare(TNodeId x, TNodeId y)
        {
            // two equal nodes must be same distance from target
            if (x.Equals(y))
                return 0;

            var a = (Span<byte>)stackalloc byte[KNodeId<TNodeId>.SizeOf];
            x.Write(a);

            var b = (Span<byte>)stackalloc byte[KNodeId<TNodeId>.SizeOf];
            y.Write(b);

            // who is closest?
            return a.SequenceCompareTo(b);
        }

    }

}
