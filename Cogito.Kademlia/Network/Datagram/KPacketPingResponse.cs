using System;
using System.Buffers;
using System.Linq;

namespace Cogito.Kademlia.Network.Datagram
{

    /// <summary>
    /// Describes a PING response.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly ref struct KPacketPingResponse<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Gets the size of the packet given the inputs.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <returns></returns>
        public static void Write(IBufferWriter<byte> writer, KPingResponse<TKNodeId> response)
        {
            KPacketIpEndpointSequence.Write(writer, response.Endpoints.OfType<KIpProtocolEndpoint<TKNodeId>>().ToArray());
        }

        readonly ReadOnlySpan<byte> span;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="span"></param>
        public KPacketPingResponse(ReadOnlySpan<byte> span)
        {
            this.span = span;
        }

        /// <summary>
        /// Gets the set of endpoints in the request.
        /// </summary>
        public KPacketIpEndpointSequence Endpoints
        {
            get => new KPacketIpEndpointSequence(span.Slice(KPacketPingResponseInfo<TKNodeId>.EndpointsOffset));
        }

    }

}
