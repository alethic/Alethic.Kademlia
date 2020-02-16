using System;
using System.Buffers.Binary;

namespace Cogito.Kademlia.Network
{

    /// <summary>
    /// Describes an IPv4 address.
    /// </summary>
    public readonly struct KIp4Address
    {

        readonly uint a;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="a"></param>
        public KIp4Address(uint a)
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
        public KIp4Address(byte a, byte b, byte c, byte d) :
            this(((uint)a << 24) | ((uint)b << 16) | ((uint)c << 8) | (uint)d)
        {

        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="input"></param>
        public KIp4Address(ReadOnlySpan<byte> input) :
            this(BinaryPrimitives.ReadUInt32BigEndian(input))
        {

        }

        /// <summary>
        /// Writes the address to the target in big endian format.
        /// </summary>
        /// <param name="target"></param>
        public void WriteTo(Span<byte> target)
        {
            BinaryPrimitives.WriteUInt32BigEndian(target, a);
        }

    }

}
