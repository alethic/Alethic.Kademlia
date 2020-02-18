using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

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
            fixed (byte* d = data)
                id.CopyTo(new Span<byte>(d, SIZE));
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
            fixed (byte* lptr = data)
            {
                var l = new ReadOnlySpan<byte>(lptr, SIZE);
                var r = new ReadOnlySpan<byte>(other.data, SIZE);
                return l.SequenceEqual(r);
            }
        }

    }

}
