using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

using Cogito.Memory;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Represents a generic 256-bit Node ID.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct KNodeId256
    {

        const int SIZE = 32;

        [FieldOffset(0)]
        fixed byte data[SIZE];

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        public KNodeId256(ulong a, ulong b, ulong c, ulong d)
        {
            fixed (byte* ptr = data)
            {
                var s = new Span<byte>(ptr, SIZE);
                BinaryPrimitives.WriteUInt64BigEndian(s, a);
                BinaryPrimitives.WriteUInt64BigEndian(s = s.Slice(sizeof(ulong)), b);
                BinaryPrimitives.WriteUInt64BigEndian(s = s.Slice(sizeof(ulong)), c);
                BinaryPrimitives.WriteUInt64BigEndian(s.Slice(sizeof(ulong)), d);
            }
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        /// <param name="e"></param>
        /// <param name="f"></param>
        /// <param name="g"></param>
        /// <param name="h"></param>
        public KNodeId256(uint a, uint b, uint c, uint d, uint e, uint f, uint g, uint h) :
            this(((ulong)a << 32) + b, ((ulong)c << 32) + d, ((ulong)e << 32) + f, ((ulong)g << 32) + h)
        {

        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="id"></param>
        public KNodeId256(ReadOnlySpan<byte> id)
        {
            fixed (byte* d = data)
                id.CopyTo(new Span<byte>(d, SIZE));
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="id"></param>
        public KNodeId256(ReadOnlySpan<ulong> id) :
            this(id[0], id[1], id[2], id[3])
        {

        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="id"></param>
        public KNodeId256(ReadOnlySpan<uint> id) :
            this(id[0], id[1], id[2], id[3], id[4], id[5], id[6], id[7])
        {

        }

        /// <summary>
        /// Compares this node ID to another node ID.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(KNodeId256 other)
        {
            fixed (byte* lptr = data)
            {
                var l = new ReadOnlySpan<byte>(lptr, SIZE);
                var r = new ReadOnlySpan<byte>(other.data, SIZE);
                return l.SequenceEqual(r);
            }
        }

        /// <summary>
        /// Compares this instance to the other instance.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(KNodeId256 other)
        {
            return KNodeIdComparer<KNodeId256>.Default.Compare(this, other);
        }

        /// <summary>
        /// Returns a string representation of this node ID.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            fixed (byte* lptr = data)
                return new ReadOnlySpan<byte>(lptr, SIZE).ToHexString();
        }

    }

}
