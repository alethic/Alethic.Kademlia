using System;

namespace Cogito.Kademlia.Network.Protocol.Datagram
{

    /// <summary>
    /// Describes the data to send for a PING request.
    /// </summary>
    public readonly ref struct KPingRequestBody<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly ReadOnlySpan<KIpEndpoint> endpoints;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="endpoints"></param>
        public KPingRequestBody(ReadOnlySpan<KIpEndpoint> endpoints)
        {
            this.endpoints = endpoints;
        }

        /// <summary>
        /// Gets the endpoints currently exported by the peer.
        /// </summary>
        public ReadOnlySpan<KIpEndpoint> Endpoints => endpoints;

    }

}
