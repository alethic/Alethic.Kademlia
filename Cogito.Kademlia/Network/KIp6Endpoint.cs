namespace Cogito.Kademlia.Network
{

    /// <summary>
    /// Describes an IPv6 endpoint.
    /// </summary>
    public readonly struct KIp6Endpoint
    {

        readonly KIp6Address address;
        readonly ushort port;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public KIp6Endpoint(KIp6Address address, ushort port)
        {
            this.address = address;
            this.port = port;
        }

        public KIp6Address Address => address;

        public ushort Port => port;

    }

}