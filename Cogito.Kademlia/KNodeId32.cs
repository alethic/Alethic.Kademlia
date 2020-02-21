using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

using Cogito.Kademlia.Core;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Represents a generic 32-bit Node ID.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct KNodeId32 : IKNodeId<KNodeId32>
    {

        const int SIZE = 4;

        [FieldOffset(0)]
        fixed byte data[SIZE];

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="a"></param>
        public KNodeId32(uint a)
        {
            fixed (byte* ptr = data)
            {
                var s = new Span<byte>(ptr, SIZE);
                BinaryPrimitives.WriteUInt32BigEndian(s, a);
            }
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="id"></param>
        public KNodeId32(ReadOnlySpan<byte> id)
        {
            fixed (byte* ptr = data)
                id.CopyTo(new Span<byte>(ptr, SIZE));
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="id"></param>
        public KNodeId32(ReadOnlySpan<uint> id) :
            this(id[0])
        {

        }

        /// <summary>
        /// Compares this node ID to another node ID.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(KNodeId32 other)
        {
            fixed (byte* ptr = data)
            {
                var l = new ReadOnlySpan<byte>(ptr, SIZE);
                var r = new ReadOnlySpan<byte>(other.data, SIZE);
                return l.SequenceEqual(r);
            }
        }

        /// <summary>
        /// Compares this instance to the other instance.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(KNodeId32 other)
        {
            return KNodeIdComparer<KNodeId32>.Default.Compare(this, other);
        }

        /// <summary>
        /// Returns a string representation of this node ID.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            fixed (byte* ptr = data)
                return new ReadOnlySpan<byte>(ptr, SIZE).ToHexString();
        }

    }

}
