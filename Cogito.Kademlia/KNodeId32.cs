using System;
using System.Buffers.Binary;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Represents a generic 32-bit Node ID.
    /// </summary>
    public readonly struct KNodeId32 : IKNodeId<KNodeId32>
    {

        readonly uint a;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="a"></param>
        public KNodeId32(uint a)
        {
            this.a = a;
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
        /// Gets the length of the node ID.
        /// </summary>
        public int DistanceSize => 32;

        /// <summary>
        /// Compares this node ID to another node ID.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(KNodeId32 other)
        {
            return a == other.a;
        }

        /// <summary>
        /// Returns the distance between this node ID and the other node ID as a series of unsigned integers.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public void CalculateDistance(KNodeId32 other, Span<byte> output)
        {
            BinaryPrimitives.WriteUInt32BigEndian(output, a ^ other.a);
        }

    }

}
