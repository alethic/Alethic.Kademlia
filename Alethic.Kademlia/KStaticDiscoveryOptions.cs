using System;

namespace Alethic.Kademlia
{

    /// <summary>
    /// Options available to the UDP static discovery service.
    /// </summary>
    public class KStaticDiscoveryOptions
    {

        /// <summary>
        /// Endpoints to engage for discovery.
        /// </summary>
        public Uri[] Endpoints { get; set; }

        /// <summary>
        /// Amount of time between runs of the discovery process.
        /// </summary>
        public TimeSpan Frequency { get; set; } = TimeSpan.FromMinutes(10);

    }

}