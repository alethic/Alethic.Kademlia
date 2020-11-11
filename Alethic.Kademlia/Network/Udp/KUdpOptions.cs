using System;
using System.Net;

namespace Alethic.Kademlia.Network.Udp
{

    /// <summary>
    /// Options available to the UDP protocol.
    /// </summary>
    public class KUdpOptions
    {

        /// <summary>
        /// Gets the set of endpoints on which to listen. If not specified, all compatible interfaces are initialized.
        /// </summary>
        public IPEndPoint Bind { get; set; } = null;

        /// <summary>
        /// Amount of time to wait for a response to a UDP packet.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Options for multicast discovery.
        /// </summary>
        public KUdpMulticastDiscoveryOptions Multicast { get; set; } = new KUdpMulticastDiscoveryOptions();

    }

}
