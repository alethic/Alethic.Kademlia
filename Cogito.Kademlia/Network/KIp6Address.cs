using System;
using System.Buffers.Binary;

namespace Cogito.Kademlia.Network
{

    /// <summary>
    /// Describes an IPv6 address.
    /// </summary>
    public readonly struct KIp6Address
    {

        readonly uint a;
        readonly uint b;
        readonly uint c;
        readonly uint d;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        public KIp6Address(uint a, uint b, uint c, uint d)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="input"></param>
        public KIp6Address(ReadOnlySpan<byte> input) :
            this(
                BinaryPrimitives.ReadUInt32BigEndian(input),
                BinaryPrimitives.ReadUInt32BigEndian(input.Slice(8)),
                BinaryPrimitives.ReadUInt32BigEndian(input.Slice(16)),
                BinaryPrimitives.ReadUInt32BigEndian(input.Slice(24)))
        {

        }

        /// <summary>
        /// Writes the address to the target in big endian format.
        /// </summary>
        /// <param name="target"></param>
        public void WriteTo(Span<byte> target)
        {
            BinaryPrimitives.WriteUInt32BigEndian(target, a);
            BinaryPrimitives.WriteUInt32BigEndian(target = target.Slice(sizeof(uint)), b);
            BinaryPrimitives.WriteUInt32BigEndian(target = target.Slice(sizeof(uint)), c);
            BinaryPrimitives.WriteUInt32BigEndian(target.Slice(sizeof(uint)), d);
        }

    }

}
