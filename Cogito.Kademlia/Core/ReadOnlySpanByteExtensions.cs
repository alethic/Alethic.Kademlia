using System;
using System.Buffers.Binary;

namespace Cogito.Kademlia.Core
{

    /// <summary>
    /// Provides extension methods against a <see cref="ReadOnlySpan{byte}"/>.
    /// </summary>
    internal static class ReadOnlySpanByteExtensions
    {

        /// <summary>
        /// Gets the number of leading zeros of the input.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static int CountLeadingZeros(this ReadOnlySpan<byte> input)
        {
            var z = 0;
            var s = input;

            while (s.Length > 0)
            {
                if (s.Length >= 8)
                {
                    var t = CountLeadingZeros64(ref s);
                    z += t;
                    if (t < 64)
                        return z;
                }
                else if (s.Length >= 4)
                {
                    var t = CountLeadingZeros32(ref s);
                    z += t;
                    if (t < 32)
                        return z;
                }
                else if (s.Length >= 2)
                {
                    var t = CountLeadingZeros16(ref s);
                    z += t;
                    if (t < 16)
                        return z;
                }
                else
                {
                    var t = CountLeadingZerosByte(ref s);
                    z += t;
                    if (t < 8)
                        return z;
                }
            }

            return z;
        }

        /// <summary>
        /// Gets the number of trailing zeros of the most significant unsigned long of the input.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        static int CountLeadingZeros64(ref ReadOnlySpan<byte> input)
        {
            // transform block into ulong
            var v = BinaryPrimitives.ReadUInt64BigEndian(input);
            var z = v.CountLeadingZeros();

            // eliminate read bytes
            input = input.Slice(sizeof(ulong));
            return z;
        }

        /// <summary>
        /// Gets the number of trailing zeros of the most significant unsigned integer of the input.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        static int CountLeadingZeros32(ref ReadOnlySpan<byte> input)
        {
            // transform block into uint
            var v = BinaryPrimitives.ReadUInt32BigEndian(input);
            var z = v.CountLeadingZeros();

            // eliminate read bytes
            input = input.Slice(sizeof(uint));
            return z;
        }

        /// <summary>
        /// Gets the number of trailing zeros of the most significant unsigned short of the input.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        static int CountLeadingZeros16(ref ReadOnlySpan<byte> input)
        {
            // transform block into ushort
            var v = BinaryPrimitives.ReadUInt16BigEndian(input);
            var z = v.CountLeadingZeros();

            // eliminate read bytes
            input = input.Slice(sizeof(ushort));
            return z;
        }

        /// <summary>
        /// Gets the number of trailing zeros of the most significant unsigned byte of the intput.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        static int CountLeadingZerosByte(ref ReadOnlySpan<byte> input)
        {
            // transform block into byte
            var v = input[0];
            var z = v.CountLeadingZeros();

            // eliminate read bytes
            input = input.Slice(sizeof(byte));
            return z;
        }

    }

}
