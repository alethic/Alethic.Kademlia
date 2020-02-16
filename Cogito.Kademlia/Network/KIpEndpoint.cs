using System.Runtime.InteropServices;

namespace Cogito.Kademlia.Network
{

    /// <summary>
    /// Describes an IP endpoint of a particular protocol.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct KIpEndpoint
    {

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
        public KIpEndpoint(KIp4Endpoint v4)
        {
            this.protocol = KIpProtocol.IPv4;
            this.v6 = new KIp6Endpoint();
            this.v4 = v4;
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="v6"></param>
        public KIpEndpoint(KIp6Endpoint v6)
        {
            this.protocol = KIpProtocol.IPv6;
            this.v4 = new KIp4Endpoint();
            this.v6 = v6;
        }

        /// <summary>
        /// Gets the protocol of the endpoint.
        /// </summary>
        public KIpProtocol Protocol => protocol;

        /// <summary>
        /// Gets the IPv4 version of the endpoint.
        /// </summary>
        public KIp4Endpoint V4 => v4;

        /// <summary>
        /// Gets the IPv6 version of the endpoint.
        /// </summary>
        public KIp6Endpoint V6 => v6;

    }

}
