using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using Cogito.Kademlia.Core;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Provides methods for interacting with a KNodeId.
    /// </summary>
    public static class KNodeId
    {

        static readonly RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

        /// <summary>
        /// Creates a new random node ID.
        /// </summary>
        /// <typeparam name="TKNodeId"></typeparam>
        /// <returns></returns>
        public static TKNodeId CreateNodeId<TKNodeId>()
            where TKNodeId : unmanaged, IKNodeId<TKNodeId>
        {
#if NET47 || NETSTANDARD2_0
            var b = new byte[Unsafe.SizeOf<TKNodeId>()];
#else
            var b = (Span<byte>)stackalloc byte[Unsafe.SizeOf<TKNodeId>()];
#endif
            rng.GetBytes(b);
            return MemoryMarshal.Read<TKNodeId>(b);
        }

        /// <summary>
        /// Writes the given <typeparamref name="TKNodeId"/> to the specified buffer writer.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="writer"></param>
        public static unsafe void Write<TKNodeId>(this TKNodeId self, IBufferWriter<byte> writer)
            where TKNodeId : unmanaged, IKNodeId<TKNodeId>
        {
            var s = Unsafe.SizeOf<TKNodeId>();
            Write(self, writer.GetSpan(s));
            writer.Advance(s);
        }

        /// <summary>
        /// Writes the given <typeparamref name="TKNodeId"/> to the specified buffer writer.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="target"></param>
        public static unsafe void Write<TKNodeId>(this TKNodeId self, Span<byte> target)
            where TKNodeId : unmanaged, IKNodeId<TKNodeId>
        {
            MemoryMarshal.Write(target, ref self);
        }

        /// <summary>
        /// Reads the given <typeparamref name="TKNodeId"/> from the specified data.
        /// </summary>
        /// <typeparam name="TKNodeId"></typeparam>
        /// <param name="sequence"></param>
        /// <param name="target"></param>
        public static void Read<TKNodeId>(ref ReadOnlySequence<byte> sequence, out TKNodeId target)
            where TKNodeId : unmanaged, IKNodeId<TKNodeId>
        {
            target = sequence.AdvanceOver(Unsafe.SizeOf<TKNodeId>(), MemoryMarshal.Read<TKNodeId>);
        }

        /// <summary>
        /// Calculates the distance between the two node IDs.
        /// </summary>
        /// <typeparam name="TKNodeId"></typeparam>
        /// <param name="l"></param>
        /// <param name="r"></param>
        /// <param name="o"></param>
        public unsafe static void CalculateDistance<TKNodeId>(in TKNodeId l, in TKNodeId r, Span<byte> o)
            where TKNodeId : unmanaged, IKNodeId<TKNodeId>
        {
            var s = Unsafe.SizeOf<TKNodeId>();
            if (o.Length < s)
                throw new ArgumentException("Output byte range must be greater than or equal to the size of the node IDs.");

#if NETCOREAPP3_0 || NETSTANDARD2_1
            // get binary representation of structure
            var a = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(l), 1));
            var b = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(r), 1));
#else
            var at = (Span<byte>)stackalloc byte[s];
            var bt = (Span<byte>)stackalloc byte[s];
            MemoryMarshal.Write(at, ref Unsafe.AsRef(l));
            MemoryMarshal.Write(bt, ref Unsafe.AsRef(r));
            var a = (ReadOnlySpan<byte>)at;
            var b = (ReadOnlySpan<byte>)bt;
#endif

            // perform xor
            a.Xor(b, o);
        }

    }

}
