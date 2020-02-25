using System;
using System.Buffers.Binary;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Cogito.Kademlia.Network
{

    /// <summary>
    /// Describes an IPv4 address.
    /// </summary>
    public unsafe struct KIp4Address : IEquatable<KIp4Address>
    {

        /// <summary>
        /// Returns the address used to describe any thing.
        /// </summary>
        public static readonly KIp4Address Any = new KIp4Address();

        fixed byte data[4];

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="a"></param>
        public KIp4Address(uint a)
        {
            fixed (byte* ptr = data)
            {
                var s = new Span<byte>(ptr, 4);
                BinaryPrimitives.WriteUInt32BigEndian(s, a);
            }
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        public KIp4Address(byte a, byte b, byte c, byte d)
        {
            fixed (byte* ptr = data)
            {
                var s = new Span<byte>(ptr, 4);
                s[0] = a;
                s[1] = b;
                s[2] = c;
                s[3] = d;
            }
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="input"></param>
        public KIp4Address(ReadOnlySpan<byte> input)
        {
            fixed (byte* ptr = data)
                input.CopyTo(new Span<byte>(ptr, 4));
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="address"></param>
        public KIp4Address(IPAddress address) :
            this(address.AddressFamily == AddressFamily.InterNetwork ? address.GetAddressBytes().ToArray() : throw new ArgumentException())
        {

        }

        /// <summary>
        /// Returns a <see cref="IPAddress"/> instance for this instance.
        /// </summary>
        /// <returns></returns>
        public IPAddress ToIPAddress()
        {
            fixed (byte* ptr = data)
                return new IPAddress(new ReadOnlySpan<byte>(ptr, 4).ToArray());
        }

        /// <summary>
        /// Returns <c>true</c> if the two instances are equal to each other.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj is KIp4Address addr && Equals(addr);
        }

        /// <summary>
        /// Returns <c>true</c> if the two instances are equal to each other.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(KIp4Address other)
        {
            fixed (byte* ptr = data)
                return new ReadOnlySpan<byte>(ptr, 4).SequenceEqual(new ReadOnlySpan<byte>(other.data, 4));
        }

        /// <summary>
        /// Gets a unique hash code identifying this instance.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            fixed (byte* ptr = data)
            {
                var h = new HashCode();
                var s = new ReadOnlySpan<byte>(ptr, 4);
                for (var i = 0; i < s.Length; i++)
                    h.Add(s[i]);
                return h.ToHashCode();
            }
        }

        /// <summary>
        /// Returns a string representation of this IP address.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{(uint)data[0]}.{(uint)data[1]}.{(uint)data[2]}.{(uint)data[3]}";
        }

        /// <summary>
        /// Writes the specified address.
        /// </summary>
        /// <param name="span"></param>
        public void Write(Span<byte> span)
        {
            fixed (byte* ptr = data)
                new ReadOnlySpan<byte>(ptr, 4).CopyTo(span);
        }

    }

}
