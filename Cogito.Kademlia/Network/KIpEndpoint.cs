using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Cogito.Kademlia.Network
{

    /// <summary>
    /// Describes an IP endpoint of a particular protocol.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct KIpEndpoint : IEquatable<KIpEndpoint>
    {

        /// <summary>
        /// Writes the given <typeparamref name="KIpEndpoint"/> to the specified buffer writer.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="writer"></param>
        public static unsafe void Write(KIpEndpoint self, IBufferWriter<byte> writer)
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
        public static unsafe void Write(KIpEndpoint self, Span<byte> target)
        {
            MemoryMarshal.Write(target, ref self);
        }

        public static implicit operator IPEndPoint(KIpEndpoint ep)
        {
            return ep.ToIPEndPoint();
        }

        public static implicit operator KIpEndpoint(IPEndPoint ep)
        {
            return new KIpEndpoint(ep);
        }

        [FieldOffset(0)]
        readonly KIpProtocol protocol;

        [FieldOffset(sizeof(KIpProtocol))]
        readonly KIp4Endpoint v4;

        [FieldOffset(sizeof(KIpProtocol))]
        readonly KIp6Endpoint v6;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="v4"></param>
        public KIpEndpoint(in KIp4Endpoint v4)
        {
            this.protocol = KIpProtocol.IPv4;
            this.v6 = new KIp6Endpoint();
            this.v4 = v4;
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="v6"></param>
        public KIpEndpoint(in KIp6Endpoint v6)
        {
            this.protocol = KIpProtocol.IPv6;
            this.v4 = new KIp4Endpoint();
            this.v6 = v6;
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
                    this.protocol = KIpProtocol.IPv4;
                    this.v6 = default;
                    this.v4 = new KIp4Endpoint(ep);
                    break;
                case AddressFamily.InterNetworkV6:
                    this.protocol = KIpProtocol.IPv6;
                    this.v4 = default;
                    this.v6 = new KIp6Endpoint(ep);
                    break;
                default:
                    throw new ArgumentException();
            }
        }

        /// <summary>
        /// Gets the protocol of the endpoint.
        /// </summary>
        public KIpProtocol Protocol => protocol;

        /// <summary>
        /// Gets the IPv4 version of the endpoint.
        /// </summary>
        public KIp4Endpoint V4 => protocol == KIpProtocol.IPv4 ? v4 : throw new InvalidOperationException();

        /// <summary>
        /// Gets the IPv6 version of the endpoint.
        /// </summary>
        public KIp6Endpoint V6 => protocol == KIpProtocol.IPv6 ? v6 : throw new InvalidOperationException();

        /// <summary>
        /// Returns this endpoint as an <see cref="IPEndPoint"/>.
        /// </summary>
        /// <returns></returns>
        public IPEndPoint ToIPEndPoint()
        {
            return protocol switch
            {
                KIpProtocol.IPv4 => v4.ToIPEndPoint(),
                KIpProtocol.IPv6 => v6.ToIPEndPoint(),
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
                KIpProtocol.IPv4 => other.Protocol == KIpProtocol.IPv4 && v4.Equals(other.v4),
                KIpProtocol.IPv6 => other.Protocol == KIpProtocol.IPv6 && v6.Equals(other.v6),
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

            switch (protocol)
            {
                case KIpProtocol.IPv4:
                    h.Add(v4);
                    break;
                case KIpProtocol.IPv6:
                    h.Add(v6);
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
                KIpProtocol.IPv4 => v4.ToString(),
                KIpProtocol.IPv6 => v6.ToString(),
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
