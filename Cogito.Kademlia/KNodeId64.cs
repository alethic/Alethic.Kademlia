using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

using Cogito.Memory;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Represents a generic 64-bit Node ID.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct KNodeId64
    {

        const int SIZE = 8;

        [FieldOffset(0)]
        fixed byte data[SIZE];

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="a"></param>
        public KNodeId64(ulong a)
        {
            fixed (byte* ptr = data)
            {
                var s = new Span<byte>(ptr, SIZE);
                BinaryPrimitives.WriteUInt64BigEndian(s, a);
            }
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public KNodeId64(uint a, uint b)
        {
            fixed (byte* ptr = data)
            {
                var s = new Span<byte>(ptr, SIZE);
                BinaryPrimitives.WriteUInt32BigEndian(s, a);
                BinaryPrimitives.WriteUInt32BigEndian(s.Slice(sizeof(uint)), b);
            }
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="id"></param>
        public KNodeId64(ReadOnlySpan<byte> id)
        {
            fixed (byte* d = data)
                id.CopyTo(new Span<byte>(d, SIZE));
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="id"></param>
        public KNodeId64(ReadOnlySpan<ulong> id) :
            this(id[0])
        {

        }

        /// <summary>
        /// Compares this node ID to another node ID.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(KNodeId64 other)
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
        public int CompareTo(KNodeId64 other)
        {
            return KNodeIdComparer<KNodeId64>.Default.Compare(this, other);
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
