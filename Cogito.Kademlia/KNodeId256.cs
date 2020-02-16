using System;
using System.Buffers.Binary;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Represents a generic 256-bit Node ID.
    /// </summary>
    public readonly struct KNodeId256 : IKNodeId<KNodeId256>
    {

        readonly ulong a;
        readonly ulong b;
        readonly ulong c;
        readonly ulong d;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        public KNodeId256(ulong a, ulong b, ulong c, ulong d)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
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
        /// Gets the length of the node ID.
        /// </summary>
        public int Size => 256;

        /// <summary>
        /// Compares this node ID to another node ID.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(KNodeId256 other)
        {
            return a == other.a && b == other.b && c == other.c && d == other.d;
        }

        /// <summary>
        /// Writes the value of this node ID to the specified binary output.
        /// </summary>
        /// <returns></returns>
        public void WriteTo(Span<byte> output)
        {
            BinaryPrimitives.WriteUInt64BigEndian(output, a);
            BinaryPrimitives.WriteUInt64BigEndian(output = output.Slice(sizeof(ulong)), b);
            BinaryPrimitives.WriteUInt64BigEndian(output = output.Slice(sizeof(ulong)), c);
            BinaryPrimitives.WriteUInt64BigEndian(output.Slice(sizeof(ulong)), d);
        }

    }

}
