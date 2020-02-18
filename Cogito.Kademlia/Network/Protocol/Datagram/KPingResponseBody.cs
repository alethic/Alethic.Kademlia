using System;

namespace Cogito.Kademlia.Network.Protocol.Datagram
{

    /// <summary>
    /// Describes the data to send for a PING response.
    /// </summary>
    public readonly ref struct KPingResponseBody<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly ReadOnlySpan<KIpEndpoint> endpoints;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="endpoints"></param>
        public KPingResponseBody(ReadOnlySpan<KIpEndpoint> endpoints)
        {
            this.endpoints = endpoints;
        }

        /// <summary>
        /// Gets the endpoints currently exported by the peer.
        /// </summary>
        public ReadOnlySpan<KIpEndpoint> Endpoints => endpoints;

    }

}
