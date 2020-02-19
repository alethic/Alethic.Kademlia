using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

#if NETCOREAPP3_0
using System.Runtime.Intrinsics.X86;
#endif

namespace Cogito.Kademlia.Core
{

    /// <summary>
    /// Provides extension methods against a <see cref="ReadOnlySpan{byte}"/>.
    /// </summary>
    internal static class ReadOnlySpanByteExtensions
    {

        static readonly uint[] hexLookupTable = CreateHexLookup();

        /// <summary>
        /// Creates a lookup table for byte values to hex character points.
        /// </summary>
        /// <returns></returns>
        static uint[] CreateHexLookup()
        {
            var r = new uint[256];

            for (var i = 0; i < 256; i++)
            {
                var s = i.ToString("x2");
                r[i] = s[0] + ((uint)s[1] << 16);
            }

            return r;
        }

        /// <summary>
        /// Converts the given byte span to a hexadecimal string.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public unsafe static string ToHexString(this ReadOnlySpan<byte> bytes)
        {
            var r = stackalloc char[bytes.Length * 2];

            for (var i = 0; i < bytes.Length; i++)
            {
                var val = hexLookupTable[bytes[i]];
                r[2 * i] = (char)val;
                r[2 * i + 1] = (char)(val >> 16);
            }

            return new string(r, 0, bytes.Length * 2);
        }

        /// <summary>
        /// Performs an XOR operation against two <see cref="ReadOnlySpan{byte}"/>.
        /// </summary>
        /// <param name="l"></param>
        /// <param name="r"></param>
        /// <param name="o"></param>
        public static void Xor(this ReadOnlySpan<byte> l, ReadOnlySpan<byte> r, Span<byte> o)
        {
            var s = o.Length;
            if (l.Length != s)
                throw new ArgumentException("Left span size must be equal to output size.");
            if (r.Length != s)
                throw new ArgumentException("Right span size must be equal to output size.");

#if NETCOREAPP3_0
            if (Avx2.IsSupported)
            {
                while (o.Length >= 32)
                {
                    var al = MemoryMarshal.Cast<byte, ulong>(l);
                    var rl = MemoryMarshal.Cast<byte, ulong>(r);
                    var ol = MemoryMarshal.Cast<byte, ulong>(o);

                    unsafe
                    {
                        fixed (ulong* lp = al)
                        fixed (ulong* rp = rl)
                        fixed (ulong* op = ol)
                        {
                            var av = Avx.LoadVector256(lp);
                            var bv = Avx.LoadVector256(rp);
                            var ov = Avx2.Xor(av, bv);
                            Avx.Store(op, ov);
                        }
                    }

                    l = l.Slice(32);
                    r = r.Slice(32);
                    o = o.Slice(32);
                }
            }
#endif

#if NETCOREAPP3_0
            if (Sse2.IsSupported)
            {
                while (o.Length >= 16)
                {
                    var ll = MemoryMarshal.Cast<byte, ulong>(l);
                    var rl = MemoryMarshal.Cast<byte, ulong>(r);
                    var ol = MemoryMarshal.Cast<byte, ulong>(o);

                    unsafe
                    {
                        fixed (ulong* lp = ll)
                        fixed (ulong* rp = rl)
                        fixed (ulong* op = ol)
                        {
                            var av = Sse2.LoadVector128(lp);
                            var bv = Sse2.LoadVector128(rp);
                            var ov = Sse2.Xor(av, bv);
                            Sse2.Store(op, ov);
                        }
                    }

                    l = l.Slice(16);
                    r = r.Slice(16);
                    o = o.Slice(16);
                }
            }
#endif

            while (o.Length >= sizeof(ulong))
            {
                var ll = MemoryMarshal.Cast<byte, ulong>(l);
                var rl = MemoryMarshal.Cast<byte, ulong>(r);
                var ol = MemoryMarshal.Cast<byte, ulong>(o);

                ol[0] = ll[0] ^ rl[0];

                l = l.Slice(sizeof(ulong));
                r = r.Slice(sizeof(ulong));
                o = o.Slice(sizeof(ulong));
            }

            while (o.Length >= sizeof(uint))
            {
                var ll = MemoryMarshal.Cast<byte, uint>(l);
                var rl = MemoryMarshal.Cast<byte, uint>(r);
                var ol = MemoryMarshal.Cast<byte, uint>(o);

                ol[0] = ll[0] ^ rl[0];

                l = l.Slice(sizeof(uint));
                r = r.Slice(sizeof(uint));
                o = o.Slice(sizeof(uint));
            }

            // finish remaining bytes
            if (o.Length > 0)
                for (var i = 0; i < o.Length; i++)
                    o[i] = (byte)(l[i] ^ r[i]);
        }

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
