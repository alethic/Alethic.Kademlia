namespace Cogito.Kademlia.Network
{

    /// <summary>
    /// Describes an IPv4 endpoint.
    /// </summary>
    public readonly struct KIp4Endpoint
    {

        readonly KIp4Address address;
        readonly ushort port;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public KIp4Endpoint(KIp4Address address, ushort port)
        {
            this.address = address;
            this.port = port;
        }

        public KIp4Address Address => address;

        public ushort Port => port;

    }

}
