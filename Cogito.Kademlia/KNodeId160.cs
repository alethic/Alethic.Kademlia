using System;
using System.Buffers.Binary;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Represents a generic 160-bit Node ID.
    /// </summary>
    public readonly struct KNodeId160 : IKNodeId<KNodeId160>
    {

        readonly ulong a;
        readonly ulong b;
        readonly uint c;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        public KNodeId160(ulong a, ulong b, uint c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
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
        public KNodeId160(ReadOnlySpan<uint> id) :
            this(id[0], id[1], id[2], id[3], id[4])
        {

        }

        /// <summary>
        /// Gets the length of the node ID.
        /// </summary>
        public int Size => 160;

        /// <summary>
        /// Compares this node ID to another node ID.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(KNodeId160 other)
        {
            return a == other.a && b == other.b && c == other.c;
        }

        /// <summary>
        /// Writes the value of this node ID to the specified binary output.
        /// </summary>
        /// <returns></returns>
        public void WriteTo(Span<byte> output)
        {
            BinaryPrimitives.WriteUInt64BigEndian(output, a);
            BinaryPrimitives.WriteUInt64BigEndian(output = output.Slice(sizeof(ulong)), b);
            BinaryPrimitives.WriteUInt32BigEndian(output.Slice(sizeof(ulong)), c);
        }

    }

}
