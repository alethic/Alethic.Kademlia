using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Cogito.Kademlia.Net
{

    /// <summary>
    /// Describes an IP endpoint of a particular protocol.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct KIpEndpoint : IEquatable<KIpEndpoint>
    {

        public static implicit operator IPEndPoint(KIpEndpoint endpoint)
        {
            return endpoint.ToIPEndPoint();
        }

        public static implicit operator KIpEndpoint(IPEndPoint endpoint)
        {
            return new KIpEndpoint(endpoint);
        }

        public static bool operator ==(KIpEndpoint a, KIpEndpoint b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(KIpEndpoint a, KIpEndpoint b)
        {
            return !a.Equals(b);
        }

        /// <summary>
        /// Unspecified protocol any address.
        /// </summary>
        public static readonly KIpEndpoint Any = new KIpEndpoint();

        /// <summary>
        /// Returns the address used to describe any IPv4 endpoint.
        /// </summary>
        public static readonly KIpEndpoint AnyV4 = new KIpEndpoint(KIp4Address.Any, 0);

        /// <summary>
        /// Returns the address used to describe any IPv6 endpoint.
        /// </summary>
        public static readonly KIpEndpoint AnyV6 = new KIpEndpoint(KIp6Address.Any, 0);

        /// <summary>
        /// Reads a <see cref="KIpEndpoint"/> from the given span.
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static KIpEndpoint Read(ReadOnlySpan<byte> span)
        {
            return MemoryMarshal.Read<KIpEndpoint>(span);
        }

        /// <summary>
        /// Writes the given <typeparamref name="KIpEndpoint"/> to the specified buffer writer.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="writer"></param>
        public static void Write(KIpEndpoint self, IBufferWriter<byte> writer)
        {
            var s = Unsafe.SizeOf<KIpEndpoint>();
            Write(self, writer.GetSpan(s));
            writer.Advance(s);
        }

        /// <summary>
        /// Writes the given <typeparamref name="KIpEndpoint"/> to the specified buffer writer.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="target"></param>
        public static void Write(KIpEndpoint self, Span<byte> target)
        {
            MemoryMarshal.Write(target, ref self);
        }

        [FieldOffset(0)]
        readonly KIpAddressFamily protocol;

        [FieldOffset(4)]
        readonly KIp4Address ipv4;

        [FieldOffset(4)]
        readonly KIp6Address ipv6;

        [FieldOffset(20)]
        readonly KIpPort port;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="ipv4"></param>
        /// <param name="port"></param>
        public KIpEndpoint(in KIp4Address ipv4, in KIpPort port)
        {
            this.protocol = KIpAddressFamily.IPv4;
            this.ipv6 = new KIp6Address();
            this.ipv4 = ipv4;
            this.port = port;
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="ipv6"></param>
        /// <param name="port"></param>
        public KIpEndpoint(in KIp6Address ipv6, in KIpPort port)
        {
            this.protocol = KIpAddressFamily.IPv6;
            this.ipv4 = new KIp4Address();
            this.ipv6 = ipv6;
            this.port = port;
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="port"></param>
        public KIpEndpoint(in KIpPort port)
        {
            this.protocol = KIpAddressFamily.Unknown;
            this.ipv6 = default;
            this.ipv4 = default;
            this.port = port;
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="ep"></param>
        public KIpEndpoint(IPEndPoint ep)
        {
            switch (ep.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    protocol = KIpAddressFamily.IPv4;
                    ipv6 = default;
                    ipv4 = new KIp4Address(ep.Address);
                    port = ep.Port;
                    break;
                case AddressFamily.InterNetworkV6:
                    protocol = KIpAddressFamily.IPv6;
                    ipv4 = default;
                    ipv6 = new KIp6Address(ep.Address);
                    port = ep.Port;
                    break;
                default:
                    throw new ArgumentException();
            }
        }

        /// <summary>
        /// Gets the protocol of the endpoint.
        /// </summary>
        public KIpAddressFamily Protocol => protocol;

        /// <summary>
        /// Gets the IPv4 version of the address.
        /// </summary>
        public KIp4Address V4 => protocol == KIpAddressFamily.IPv4 ? ipv4 : throw new InvalidOperationException();

        /// <summary>
        /// Gets the IPv6 version of the address.
        /// </summary>
        public KIp6Address V6 => protocol == KIpAddressFamily.IPv6 ? ipv6 : throw new InvalidOperationException();

        /// <summary>
        /// Gets the port of the endpoint.
        /// </summary>
        public KIpPort Port => port;

        /// <summary>
        /// Returns this endpoint as an <see cref="IPEndPoint"/>.
        /// </summary>
        /// <returns></returns>
        public IPEndPoint ToIPEndPoint()
        {
            return protocol switch
            {
                KIpAddressFamily.IPv4 => new IPEndPoint(ipv4.ToIPAddress(), port),
                KIpAddressFamily.IPv6 => new IPEndPoint(ipv6.ToIPAddress(), port),
                _ => throw new InvalidOperationException(),
            };
        }

        /// <summary>
        /// Returns <c>true</c> if the two instances are equal to each other.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj is KIpEndpoint ep && Equals(ep);
        }

        /// <summary>
        /// Returns <c>true</c> if the two instances are equal to each other.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(KIpEndpoint other)
        {
            return protocol switch
            {
                KIpAddressFamily.IPv4 => other.Protocol == KIpAddressFamily.IPv4 && ipv4.Equals(other.ipv4) && port.Equals(other.port),
                KIpAddressFamily.IPv6 => other.Protocol == KIpAddressFamily.IPv6 && ipv6.Equals(other.ipv6) && port.Equals(other.port),
                _ => false,
            };
        }

        /// <summary>
        /// Gets a unique hash code identifying this instance.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            var h = new HashCode();
            h.Add(protocol);
            h.Add(port);

            switch (protocol)
            {
                case KIpAddressFamily.IPv4:
                    h.Add(ipv4);
                    break;
                case KIpAddressFamily.IPv6:
                    h.Add(ipv6);
                    break;
                default:
                    throw new InvalidOperationException();
            };

            return h.ToHashCode();
        }

        /// <summary>
        /// Returns a string representation of this IP endpoint.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return protocol switch
            {
                KIpAddressFamily.IPv4 => $"{ipv4}:{port}",
                KIpAddressFamily.IPv6 => $"[{ipv6}]:{port}",
                _ => null,
            };
        }

        /// <summary>
        /// Writes this <typeparamref name="KIpEndpoint"/> to the specified buffer writer.
        /// </summary>
        /// <param name="writer"></param>
        public void Write(IBufferWriter<byte> writer)
        {
            Write(this, writer);
        }

        /// <summary>
        /// Writes this <typeparamref name="KIpEndpoint"/> to the specified span.
        /// </summary>
        /// <param name="span"></param>
        public void Write(Span<byte> span)
        {
            Write(this, span);
        }

    }

}
