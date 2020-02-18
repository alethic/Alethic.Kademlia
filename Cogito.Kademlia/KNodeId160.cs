using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Represents a generic 160-bit Node ID.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct KNodeId160 : IKNodeId<KNodeId160>
    {

        const int SIZE = 20;

        [FieldOffset(0)]
        fixed byte data[SIZE];

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        public KNodeId160(ulong a, ulong b, uint c)
        {
            fixed (byte* ptr = data)
            {
                var s = new Span<byte>(ptr, SIZE);
                BinaryPrimitives.WriteUInt64BigEndian(s, a);
                BinaryPrimitives.WriteUInt64BigEndian(s = s.Slice(sizeof(ulong)), b);
                BinaryPrimitives.WriteUInt32BigEndian(s.Slice(sizeof(ulong)), c);
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
        public KNodeId160(uint a, uint b, uint c, uint d, uint e) :
            this(((ulong)a << 32) + b, ((ulong)c << 32) + d, e)
        {

        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="id"></param>
        public KNodeId160(ReadOnlySpan<byte> id)
        {
            fixed (byte* d = data)
                id.CopyTo(new Span<byte>(d, SIZE));
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="id"></param>
        public KNodeId160(ReadOnlySpan<uint> id) :
            this(id[0], id[1], id[2], id[3], id[4])
        {

        }

        /// <summary>
        /// Compares this node ID to another node ID.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(KNodeId160 other)
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
