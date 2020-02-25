using System;

namespace Cogito.Kademlia.Core
{

    /// <summary>
    /// Provides extension methods against a <see cref="Span{byte}"/>.
    /// </summary>
    internal static class SpanByteExtensions
    {

        /// <summary>
        /// Gets the bit at the specified position in <paramref name="span"/>.
        /// </summary>
        /// <param name="span"></param>
        /// <param name="index"></param>
        public static bool GetBit(this Span<byte> span, int index)
        {
            return ((ReadOnlySpan<byte>)span).GetBit(index);
        }

        /// <summary>
        /// Sets the bit at the specified position in <paramref name="span"/> to <paramref name="value"/>.
        /// </summary>
        /// <param name="span"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void SetBit(this Span<byte> span, int index, bool value)
        {
            var m = (byte)((uint)1 << (8 - index % 8 - 1));
            if (value)
                span[index / 8] |= m;
            else
                span[index / 8] &= (byte)~m;
        }

        /// <summary>
        /// Performs an AND operation against two <see cref="ReadOnlySpan{byte}"/>.
        /// </summary>
        /// <param name="l"></param>
        /// <param name="r"></param>
        /// <param name="o"></param>
        public static void And(this Span<byte> l, ReadOnlySpan<byte> r, Span<byte> o)
        {
            ((ReadOnlySpan<byte>)l).And(r, o);
        }

        /// <summary>
        /// Performs an XOR operation against two <see cref="ReadOnlySpan{byte}"/>.
        /// </summary>
        /// <param name="l"></param>
        /// <param name="r"></param>
        /// <param name="o"></param>
        public static void Xor(this Span<byte> l, ReadOnlySpan<byte> r, Span<byte> o)
        {
            ((ReadOnlySpan<byte>)l).Xor(r, o);
        }

    }

}
