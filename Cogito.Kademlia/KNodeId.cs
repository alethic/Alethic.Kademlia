using System;
using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using Cogito.Kademlia.Core;
using Cogito.Memory;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Provides methods for interacting with a KNodeId.
    /// </summary>
    public static class KNodeId<TNodeId>
        where TNodeId : unmanaged
    {

        static readonly int szz = Unsafe.SizeOf<TNodeId>();
        static readonly RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

        /// <summary>
        /// Gets the size of the <typeparamref name="TNodeId"/> type.
        /// </summary>
        /// <returns></returns>
        public static int SizeOf => szz;

        /// <summary>
        /// Creates a new random node ID.
        /// </summary>
        /// <returns></returns>
        public static TNodeId Create()
        {
#if NET47 || NETSTANDARD2_0
            var b = new byte[SizeOf];
#else
            var b = (Span<byte>)stackalloc byte[SizeOf];
#endif
            rng.GetBytes(b);
            return MemoryMarshal.Read<TNodeId>(b);
        }

        /// <summary>
        /// Reads the given <typeparamref name="TNodeId"/> from the specified data.
        /// </summary>
        /// <param name="sequence"></param>
        public static TNodeId Read(ref ReadOnlySequence<byte> sequence)
        {
            return sequence.AdvanceOver(SizeOf, Read);
        }

        /// <summary>
        /// Reads the given <typeparamref name="TNodeId"/> from the specified data.
        /// </summary>
        /// <param name="span"></param>
        public static TNodeId Read(ReadOnlySpan<byte> span)
        {
            return MemoryMarshal.Read<TNodeId>(span);
        }

        /// <summary>
        /// Writes the given <typeparamref name="TNodeId"/> to the specified buffer writer.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="writer"></param>
        public static void Write(TNodeId self, IBufferWriter<byte> writer)
        {
            var s = SizeOf;
            Write(self, writer.GetSpan(s));
            writer.Advance(s);
        }

        /// <summary>
        /// Writes the given <typeparamref name="TNodeId"/> to the specified buffer writer.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="target"></param>
        public static void Write(TNodeId self, Span<byte> target)
        {
            MemoryMarshal.Write(target, ref self);
        }

        /// <summary>
        /// Calculates the distance between the two node IDs.
        /// </summary>
        /// <typeparam name="TNodeId"></typeparam>
        /// <param name="l"></param>
        /// <param name="r"></param>
        /// <param name="o"></param>
        public static void CalculateDistance(in TNodeId l, in TNodeId r, Span<byte> o)
        {
            var s = SizeOf;
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

        /// <summary>
        /// Randomizes the last <paramref name="suffixCount"/> bits within a <see cref="TNodeId"/>.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="suffixCount"></param>
        /// <returns></returns>
        public static TNodeId Randomize(in TNodeId self, int suffixCount)
        {
            // set all except suffix
            var selfMask = new BitArray(SizeOf * 8);
            for (int i = 0; i < selfMask.Length - suffixCount; i++)
                selfMask.Set(i, true);

            // set only suffix
            var randMask = new BitArray(selfMask).Not();

            var selfNode = (Span<byte>)stackalloc byte[SizeOf];
            self.Write(selfNode);
            var selfBuff = new BitArray(selfNode.ToArray());
            selfBuff.And(selfMask);

            var randNode = (Span<byte>)stackalloc byte[SizeOf];
            Create().Write(randNode);
            var randBuff = new BitArray(randNode.ToArray());
            randBuff.And(randMask);

            var cmplBuff = selfBuff.Or(randBuff);
            var cmplNode = new byte[SizeOf];
            cmplBuff.CopyTo(cmplNode, 0);
            var cmplFins = Read(cmplNode);

            return cmplFins;
        }

    }

    /// <summary>
    /// Provides methods for interacting with a KNodeId.
    /// </summary>
    public static class KNodeId
    {

        /// <summary>
        /// Creates a new random node ID.
        /// </summary>
        /// <returns></returns>
        public static TNodeId Create<TNodeId>()
            where TNodeId : unmanaged
        {
            return KNodeId<TNodeId>.Create();
        }

        /// <summary>
        /// Writes the given <typeparamref name="TNodeId"/> to the specified buffer writer.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="writer"></param>
        public static void Write<TNodeId>(this TNodeId self, IBufferWriter<byte> writer)
            where TNodeId : unmanaged
        {
            KNodeId<TNodeId>.Write(self, writer);
        }

        /// <summary>
        /// Writes the given <typeparamref name="TNodeId"/> to the specified buffer writer.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="target"></param>
        public static void Write<TNodeId>(this TNodeId self, Span<byte> target)
            where TNodeId : unmanaged
        {
            KNodeId<TNodeId>.Write(self, target);
        }

        /// <summary>
        /// Calculates the distance between the two node IDs.
        /// </summary>
        /// <typeparam name="TNodeId"></typeparam>
        /// <param name="l"></param>
        /// <param name="r"></param>
        /// <param name="o"></param>
        public static void CalculateDistance<TNodeId>(in TNodeId l, in TNodeId r, Span<byte> o)
            where TNodeId : unmanaged
        {
            KNodeId<TNodeId>.CalculateDistance(l, r, o);
        }

    }

}
