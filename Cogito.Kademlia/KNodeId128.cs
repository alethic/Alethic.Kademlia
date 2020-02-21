using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

using Cogito.Kademlia.Core;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Represents a generic 128-bit Node ID.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct KNodeId128 : IKNodeId<KNodeId128>
    {

        const int SIZE = 16;

        [FieldOffset(0)]
        fixed byte data[SIZE];

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public KNodeId128(ulong a, ulong b)
        {
            fixed (byte* ptr = data)
            {
                var s = new Span<byte>(ptr, SIZE);
                BinaryPrimitives.WriteUInt64BigEndian(s, a);
                BinaryPrimitives.WriteUInt64BigEndian(s.Slice(sizeof(ulong)), b);
            }
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        public KNodeId128(uint a, uint b, uint c, uint d)
        {
            fixed (byte* ptr = data)
            {
                var s = new Span<byte>(ptr, SIZE);
                BinaryPrimitives.WriteUInt32BigEndian(s, a);
                BinaryPrimitives.WriteUInt32BigEndian(s = s.Slice(sizeof(uint)), b);
                BinaryPrimitives.WriteUInt32BigEndian(s = s.Slice(sizeof(uint)), c);
                BinaryPrimitives.WriteUInt32BigEndian(s.Slice(sizeof(uint)), d);
            }
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="id"></param>
        public KNodeId128(ReadOnlySpan<byte> id)
        {
            fixed (byte* d = data)
                id.CopyTo(new Span<byte>(d, SIZE));
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="id"></param>
        public KNodeId128(ReadOnlySpan<ulong> id) :
            this(id[0], id[1])
        {

        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="id"></param>
        public KNodeId128(ReadOnlySpan<uint> id) :
            this(id[0], id[1], id[2], id[3])
        {

        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="id"></param>
        public KNodeId128(Guid id) :
            this(id.ToByteArray())
        {

        }

        /// <summary>
        /// Compares this node ID to another node ID.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(KNodeId128 other)
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
        public int CompareTo(KNodeId128 other)
        {
            return KNodeIdComparer<KNodeId128>.Default.Compare(this, other);
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
