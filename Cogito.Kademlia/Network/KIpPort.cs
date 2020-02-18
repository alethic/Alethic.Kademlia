using System;
using System.Buffers.Binary;

namespace Cogito.Kademlia.Network
{

    /// <summary>
    /// Describes an IPv4 address.
    /// </summary>
    public unsafe struct KIpPort : IEquatable<KIpPort>
    {

        /// <summary>
        /// Implicitly casts this instance as a <see cref="ushort"/>.
        /// </summary>
        /// <param name="port"></param>
        public static implicit operator ushort(KIpPort port)
        {
            return port.Value;
        }

        /// <summary>
        /// Implicitly casts this instance as a <see cref="ushort"/>.
        /// </summary>
        /// <param name="port"></param>
        public static implicit operator KIpPort(ushort port)
        {
            return new KIpPort(port);
        }

        /// <summary>
        /// Implicitly casts this instance as a <see cref="uint"/>.
        /// </summary>
        /// <param name="port"></param>
        public static implicit operator uint(KIpPort port)
        {
            return port.Value;
        }

        /// <summary>
        /// Implicitly casts this instance as a <see cref="uint"/>.
        /// </summary>
        /// <param name="port"></param>
        public static implicit operator KIpPort(uint port)
        {
            return new KIpPort(port);
        }

        /// <summary>
        /// Implicitly casts this instance as a <see cref="int"/>.
        /// </summary>
        /// <param name="port"></param>
        public static implicit operator int(KIpPort port)
        {
            return port.Value;
        }

        /// <summary>
        /// Implicitly casts this instance as a <see cref="int"/>.
        /// </summary>
        /// <param name="port"></param>
        public static implicit operator KIpPort(int port)
        {
            return new KIpPort(port);
        }

        fixed byte data[2];

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="a"></param>
        public KIpPort(ushort a)
        {
            fixed (byte* ptr = data)
            {
                var s = new Span<byte>(ptr, 2);
                BinaryPrimitives.WriteUInt16BigEndian(s, a);
            }
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="a"></param>
        public KIpPort(int a) :
            this((ushort)a)
        {

        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="a"></param>
        public KIpPort(uint a) :
            this((ushort)a)
        {

        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        public KIpPort(byte a, byte b)
        {
            fixed (byte* ptr = data)
            {
                var s = new Span<byte>(ptr, 2);
                s[0] = a;
                s[1] = b;
            }
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="input"></param>
        public KIpPort(ReadOnlySpan<byte> input)
        {
            fixed (byte* ptr = data)
                input.CopyTo(new Span<byte>(ptr, 2));
        }

        /// <summary>
        /// Gets a numeric representation of the port.
        /// </summary>
        public ushort Value
        {
            get
            {
                fixed (byte* ptr = data)
                {
                    var s = new Span<byte>(ptr, 2);
                    return BinaryPrimitives.ReadUInt16BigEndian(s);
                }
            }
        }

        /// <summary>
        /// Returns <c>true</c> if the two instances are equal to each other.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj is KIpPort port && Equals(port);
        }

        /// <summary>
        /// Returns <c>true</c> if the two instances are equal to each other.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(KIpPort other)
        {
            return Value == other.Value;
        }

        /// <summary>
        /// Gets a unique hash code identifying this instance.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        /// <summary>
        /// Returns a string representation of this instance.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Value.ToString();
        }

    }

}
