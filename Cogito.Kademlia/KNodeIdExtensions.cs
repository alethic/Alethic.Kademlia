using System;

using Cogito.Kademlia.Core;

namespace Cogito.Kademlia
{

    public static class KNodeIdExtensions
    {

        /// <summary>
        /// Calculates the distance between the two node IDs.
        /// </summary>
        /// <typeparam name="TKNodeId"></typeparam>
        /// <param name="l"></param>
        /// <param name="r"></param>
        public unsafe static void CalculateDistance<TKNodeId>(TKNodeId l, TKNodeId r, Span<byte> o)
            where TKNodeId : struct, IKNodeId<TKNodeId>
        {
            if (l.Size != r.Size)
                throw new ArgumentException("NodeID length must be equal between left and right.");
            if (l.Size % 8 != 0)
                throw new ArgumentException("NodeID length must be divisible by 8.");
            if (r.Size % 8 != 0)
                throw new ArgumentException("NodeID length must be divisible by 8.");

            var s = l.Size / 8;
            if (o.Length < s)
                throw new ArgumentException("Output byte range must be greater than or equal to the size of the node IDs.");

            // copy binary representation of node ID
            var a = (Span<byte>)stackalloc byte[s];
            var b = (Span<byte>)stackalloc byte[s];
            l.WriteTo(a);
            r.WriteTo(b);

            // perform xor
            var az = (ReadOnlySpan<byte>)a;
            var bz = (ReadOnlySpan<byte>)b;
            az.Xor(bz, o);
        }

    }

}
