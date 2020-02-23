using System;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes information about an endpoint.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public class KEndpointInfo<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="lastSeen"></param>
        public KEndpointInfo(DateTime lastSeen)
        {
            LastSeen = lastSeen;
        }

        /// <summary>
        /// Gets or sets the last time the endpoint was seen.
        /// </summary>
        public DateTime LastSeen { get; set; }

    }

}
