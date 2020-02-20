using System;
using System.Buffers;
using System.Linq;

namespace Cogito.Kademlia.Network.Datagram
{

    /// <summary>
    /// Describes a datagram IP peer.
    /// </summary>
    public readonly ref struct KPacketPeerEndpointInfo<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Writes out a <see cref="KPacketPeerEndpointInfo{TKNodeId}"/> structure.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="peer"></param>
        public static void Write(IBufferWriter<byte> writer, KPeerEndpointInfo<TKNodeId> peer)
        {
            KNodeId<TKNodeId>.Write(peer.Id, writer);
            KPacketIpEndpointSequence.Write(writer, peer.Endpoints.OfType<KIpProtocolEndpoint<TKNodeId>>().ToArray());
        }

        readonly ReadOnlySpan<byte> span;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="span"></param>
        public KPacketPeerEndpointInfo(ReadOnlySpan<byte> span)
        {
            this.span = span;
        }

        /// <summary>
        /// Gets the ID of the peer.
        /// </summary>
        public TKNodeId Id
        {
            get => KNodeId<TKNodeId>.Read(span.Slice(KPacketPeerInfo<TKNodeId>.IdOffset, KPacketPeerInfo<TKNodeId>.IdSize));
        }

        /// <summary>
        /// Gets the IP endpoints of the peer.
        /// </summary>
        public KPacketIpEndpointSequence Endpoints
        {
            get => new KPacketIpEndpointSequence(span.Slice(KPacketPeerInfo<TKNodeId>.EndpointsOffset));
        }

    }

}
