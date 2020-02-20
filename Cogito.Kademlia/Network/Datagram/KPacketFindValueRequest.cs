using System;
using System.Buffers;

namespace Cogito.Kademlia.Network.Datagram
{

    /// <summary>
    /// Describes a FIND_VALUE request.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly ref struct KPacketFindValueRequest<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Gets the size of the packet given the inputs.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static void Write(IBufferWriter<byte> writer, KFindValueRequest<TKNodeId> request)
        {
            KNodeId<TKNodeId>.Write(request.Key, writer);
        }

        readonly ReadOnlySpan<byte> span;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="span"></param>
        public KPacketFindValueRequest(ReadOnlySpan<byte> span)
        {
            this.span = span;
        }

        /// <summary>
        /// Gets the key of the value being searched for.
        /// </summary>
        public TKNodeId Key
        {
            get => KNodeId<TKNodeId>.Read(span.Slice(KPacketFindValueRequestInfo<TKNodeId>.KeyOffset, KPacketFindValueRequestInfo<TKNodeId>.KeySize));
        }
    }

}
