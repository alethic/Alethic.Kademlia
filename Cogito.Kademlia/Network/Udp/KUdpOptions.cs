using System;
using System.Net;

namespace Cogito.Kademlia.Network.Udp
{

    /// <summary>
    /// Options available to the UDP protocol.
    /// </summary>
    public class KUdpOptions<TNodeId>
        where TNodeId : unmanaged
    {

        static readonly Random random = new Random();

        /// <summary>
        /// Magic number that uniquely identifies the Kademlia network within packets.
        /// </summary>
        public ulong Network { get; set; } = (ulong)random.NextInt64();

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
        public KUdpMulticastDiscoveryOptions<TNodeId> Multicast { get; set; } = new KUdpMulticastDiscoveryOptions<TNodeId>();

    }

}
