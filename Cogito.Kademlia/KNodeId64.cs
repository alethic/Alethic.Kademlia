using System;
using System.Buffers.Binary;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Represents a generic 64-bit Node ID.
    /// </summary>
    public readonly struct KNodeId64 : IKNodeId<KNodeId64>
    {

        readonly ulong a;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="a"></param>
        public KNodeId64(ulong a)
        {
            this.a = a;
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        public KNodeId64(uint a, uint b)
        {
            this.a = ((ulong)a << 32) + b;
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
        /// Gets the length of the node ID.
        /// </summary>
        public int DistanceSize => 64;

        /// <summary>
        /// Compares this node ID to another node ID.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(KNodeId64 other)
        {
            return a == other.a;
        }

        /// <summary>
        /// Calculates the distance between this node ID and the other node ID and outputs it to the specified destination in most signficant byte order.
        /// </summary>
        /// <param name="other"></param>
        /// <param name="output"></param>
        public void CalculateDistance(KNodeId64 other, Span<byte> output)
        {
            BinaryPrimitives.WriteUInt64BigEndian(output, a ^ other.a);
        }

    }

}
