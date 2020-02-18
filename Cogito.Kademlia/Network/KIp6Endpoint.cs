using System;
using System.Net;

namespace Cogito.Kademlia.Network
{

    /// <summary>
    /// Describes an IPv6 endpoint.
    /// </summary>
    public readonly struct KIp6Endpoint : IEquatable<KIp6Endpoint>
    {

        readonly KIp6Address address;
        readonly KIpPort port;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public KIp6Endpoint(in KIp6Address address, in KIpPort port)
        {
            this.address = address;
            this.port = port;
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="ep"></param>
        public KIp6Endpoint(IPEndPoint ep) :
            this(new KIp6Address(ep.Address), new KIpPort(ep.Port))
        {

        }

        /// <summary>
        /// Gets the address of the endpoint.
        /// </summary>
        public KIp6Address Address => address;

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
            return new IPEndPoint(address.ToIPAddress(), port);
        }

        /// <summary>
        /// Returns <c>true</c> if the two instances are equal to each other.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj is KIp6Endpoint ep && Equals(ep);
        }

        /// <summary>
        /// Returns <c>true</c> if the two instances are equal to each other.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(KIp6Endpoint other)
        {
            return address.Equals(other.Address) && port.Equals(other.Port);
        }

        /// <summary>
        /// Gets a unique hash code identifying this instance.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            var h = new HashCode();
            h.Add(address);
            h.Add(port);
            return h.ToHashCode();
        }

        /// <summary>
        /// Returns a string representation of this IP endpoint.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"[{address}]:{port}";
        }

    }

}
