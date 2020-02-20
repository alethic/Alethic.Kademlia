using System;
using System.Buffers;

namespace Cogito.Kademlia.Network.Datagram
{

    /// <summary>
    /// Describes a datagram IP endpoint.
    /// </summary>
    public readonly ref struct KPacketIpEndpoint
    {

        /// <summary>
        /// Writes the endpoint data as a <see cref="KPacketIpEndpoint"/>.
        /// </summary>
        /// <typeparam name="TKNodeId"></typeparam>
        /// <param name="writer"></param>
        /// <param name="endpoint"></param>
        public static void Write<TKNodeId>(IBufferWriter<byte> writer, KIpProtocolEndpoint<TKNodeId> endpoint)
            where TKNodeId : unmanaged, IKNodeId<TKNodeId>
        {
            endpoint.Endpoint.Write(writer);
        }

        readonly ReadOnlySpan<byte> span;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="span"></param>
        public KPacketIpEndpoint(ReadOnlySpan<byte> span)
        {
            this.span = span;
        }

        /// <summary>
        /// Gets the endpoint value.
        /// </summary>
        public KIpEndpoint Value
        {
            get => KIpEndpoint.Read(span);
        }
    }

}
