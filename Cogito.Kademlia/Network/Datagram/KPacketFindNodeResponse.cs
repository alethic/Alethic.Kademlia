using System;
using System.Buffers;

using Cogito.Kademlia.Core;

namespace Cogito.Kademlia.Network.Datagram
{

    /// <summary>
    /// Describes a FIND_NODE response.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly ref struct KPacketFindNodeResponse<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        public static void Write(ArrayBufferWriter<byte> writer, KFindNodeResponse<TKNodeId> response)
        {
            KNodeId<TKNodeId>.Write(response.Key, writer);
            KPacketPeerSequence<TKNodeId>.Write(writer, response.Peers);
        }

        readonly ReadOnlySpan<byte> span;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="span"></param>
        public KPacketFindNodeResponse(ReadOnlySpan<byte> span)
        {
            this.span = span;
        }

        /// <summary>
        /// Gets the key of the node being searched for.
        /// </summary>
        public TKNodeId Key
        {
            get => KNodeId<TKNodeId>.Read(span.Slice(KPacketFindNodeResponseInfo<TKNodeId>.KeyOffset, KPacketFindNodeResponseInfo<TKNodeId>.KeySize));
        }

        /// <summary>
        /// 
        /// </summary>
        public KPacketPeerSequence<TKNodeId> Peers
        {
            get => new KPacketPeerSequence<TKNodeId>(span.Slice(KPacketFindNodeResponseInfo<TKNodeId>.PeersOffset));
        }

    }

}
