using System;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes information about an endpoint.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public struct KEndpointInfo<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="endpoint"></param>
        public KEndpointInfo(IKEndpoint<TKNodeId> endpoint)
        {
            Endpoint = endpoint;
            LastSeen = DateTime.MinValue;
        }

        /// <summary>
        /// Gets or sets the endpoint itself.
        /// </summary>
        public IKEndpoint<TKNodeId> Endpoint { get; }

        /// <summary>
        /// Gets or sets the last time the endpoint was seen.
        /// </summary>
        public DateTime LastSeen { get; set; }

    }

}
