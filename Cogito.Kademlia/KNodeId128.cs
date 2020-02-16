using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Represents a generic 128-bit Node ID.
    /// </summary>
    public readonly struct KNodeId128 : IKNodeId<KNodeId128>
    {

        readonly ulong a;
        readonly ulong b;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public KNodeId128(ulong a, ulong b)
        {
            this.a = a;
            this.b = b;
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        public KNodeId128(uint a, uint b, uint c, uint d) :
            this(((ulong)a << 32) + b, ((ulong)c << 32) + d)
        {

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
        unsafe public KNodeId128(Guid id) :
            this(new ReadOnlySpan<ulong>(Unsafe.AsPointer(ref id), 2))
        {

        }

        /// <summary>
        /// Gets the length of the node ID.
        /// </summary>
        public int Size => 128;

        /// <summary>
        /// Compares this node ID to another node ID.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(KNodeId128 other)
        {
            return a == other.a && b == other.b;
        }

        /// <summary>
        /// Writes the value of this node ID to the specified binary output.
        /// </summary>
        /// <returns></returns>
        public void WriteTo(Span<byte> output)
        {
            BinaryPrimitives.WriteUInt64BigEndian(output, a);
            BinaryPrimitives.WriteUInt64BigEndian(output.Slice(sizeof(ulong)), b);
        }

    }

}
