using System;
using System.Net;

namespace Alethic.Kademlia.Network.Udp
{

    /// <summary>
    /// Options available to the UDP multicast discovery service.
    /// </summary>
    public class KUdpMulticastDiscoveryOptions
    {

        /// <summary>
        /// Multicast endpoint to bind.
        /// </summary>
        public IPEndPoint Endpoint { get; set; } = new IPEndPoint(KIp4Address.Parse("239.255.83.54"), 1283);

        /// <summary>
        /// Amount of time to wait for a response to a UDP packet.
        /// </summary>
        public TimeSpan? Timeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Amount of time between runs of the discovery process.
        /// </summary>
        public TimeSpan? Frequency { get; set; } = TimeSpan.FromMinutes(10);

    }

}