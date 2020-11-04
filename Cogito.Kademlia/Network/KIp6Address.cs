using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace Cogito.Kademlia.Network
{

    /// <summary>
    /// Describes an IPv6 address.
    /// </summary>
    public unsafe struct KIp6Address : IEquatable<KIp6Address>
    {

        public static implicit operator IPAddress(KIp6Address address)
        {
            return address.ToIPAddress();
        }

        public static implicit operator KIp6Address(IPAddress address)
        {
            return new KIp6Address(address);
        }

        public static bool operator ==(KIp6Address a, KIp6Address b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(KIp6Address a, KIp6Address b)
        {
            return !a.Equals(b);
        }

        public static KIp6Address Parse(ReadOnlySpan<char> text)
        {
#if NETSTANDARD2_0 || NET47
            return new KIp6Address(IPAddress.Parse(text.ToString()));
#else
            return new KIp6Address(IPAddress.Parse(text));
#endif
        }

        public static KIp6Address Parse(string text)
        {
            return new KIp6Address(IPAddress.Parse(text));
        }

        /// <summary>
        /// Returns the address used to describe any thing.
        /// </summary>
        public static readonly KIp6Address Any = new KIp6Address();

        fixed byte data[16];

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        public KIp6Address(ushort a, ushort b, ushort c, ushort d, ushort e, ushort f, ushort g, ushort h)
        {
            fixed (byte* ptr = data)
            {
                var s = new Span<byte>(ptr, 16);
                BinaryPrimitives.WriteUInt16BigEndian(s, a);
                BinaryPrimitives.WriteUInt16BigEndian(s = s.Slice(sizeof(ushort)), b);
                BinaryPrimitives.WriteUInt16BigEndian(s = s.Slice(sizeof(ushort)), c);
                BinaryPrimitives.WriteUInt16BigEndian(s = s.Slice(sizeof(ushort)), d);
                BinaryPrimitives.WriteUInt16BigEndian(s = s.Slice(sizeof(ushort)), e);
                BinaryPrimitives.WriteUInt16BigEndian(s = s.Slice(sizeof(ushort)), f);
                BinaryPrimitives.WriteUInt16BigEndian(s = s.Slice(sizeof(ushort)), g);
                BinaryPrimitives.WriteUInt16BigEndian(s = s.Slice(sizeof(ushort)), h);
            }
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        public KIp6Address(uint a, uint b, uint c, uint d)
        {
            fixed (byte* ptr = data)
            {
                var s = new Span<byte>(ptr, 16);
                BinaryPrimitives.WriteUInt32BigEndian(s, a);
                BinaryPrimitives.WriteUInt32BigEndian(s = s.Slice(sizeof(uint)), b);
                BinaryPrimitives.WriteUInt32BigEndian(s = s.Slice(sizeof(uint)), c);
                BinaryPrimitives.WriteUInt32BigEndian(s = s.Slice(sizeof(uint)), d);
            }
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="input"></param>
        public KIp6Address(ReadOnlySpan<byte> input)
        {
            fixed (byte* ptr = data)
                input.CopyTo(new Span<byte>(ptr, 16));
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="address"></param>
        public KIp6Address(IPAddress address) :
            this(address.AddressFamily == AddressFamily.InterNetworkV6 ? address.GetAddressBytes() : throw new ArgumentException())
        {

        }

        /// <summary>
        /// Returns a <see cref="IPAddress"/> instance for this instance.
        /// </summary>
        /// <returns></returns>
        public IPAddress ToIPAddress()
        {
#if NETSTANDARD2_0 || NET47
            fixed (byte* ptr = data)
                return new IPAddress(new ReadOnlySpan<byte>(ptr, 16).ToArray());
#else
            fixed (byte* ptr = data)
                return new IPAddress(new ReadOnlySpan<byte>(ptr, 16));
#endif
        }

        /// <summary>
        /// Returns <c>true</c> if the two instances are equal to each other.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj is KIp6Address addr && Equals(addr);
        }

        /// <summary>
        /// Returns <c>true</c> if the two instances are equal to each other.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(KIp6Address other)
        {
            fixed (byte* ptr = data)
                return new ReadOnlySpan<byte>(ptr, 16).SequenceEqual(new ReadOnlySpan<byte>(other.data, 16));
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
                var s = new ReadOnlySpan<byte>(ptr, 16);
                for (var i = 0; i < s.Length; i++)
                    h.Add(s[i]);
                return h.ToHashCode();
            }
        }

        /// <summary>
        /// Returns a string representation of this IP endpoint.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            fixed (byte* ptr = data)
            {
                var s = new Span<byte>(ptr, 16);
                var a = BinaryPrimitives.ReadInt16BigEndian(s);
                var b = BinaryPrimitives.ReadInt16BigEndian(s = s.Slice(sizeof(ushort)));
                var c = BinaryPrimitives.ReadInt16BigEndian(s = s.Slice(sizeof(ushort)));
                var d = BinaryPrimitives.ReadInt16BigEndian(s = s.Slice(sizeof(ushort)));
                var e = BinaryPrimitives.ReadInt16BigEndian(s = s.Slice(sizeof(ushort)));
                var f = BinaryPrimitives.ReadInt16BigEndian(s = s.Slice(sizeof(ushort)));
                var g = BinaryPrimitives.ReadInt16BigEndian(s = s.Slice(sizeof(ushort)));
                var h = BinaryPrimitives.ReadInt16BigEndian(s = s.Slice(sizeof(ushort)));
                return $"{a:x4}:{b:x4}:{c:x4}:{d:x4}:{e:x4}:{f:x4}:{g:x4}:{h:x4}";
            }
        }

        /// <summary>
        /// Writes the specified address.
        /// </summary>
        /// <param name="span"></param>
        public void Write(Span<byte> span)
        {
            fixed (byte* ptr = data)
                new ReadOnlySpan<byte>(ptr, 16).CopyTo(span);
        }

    }

}

