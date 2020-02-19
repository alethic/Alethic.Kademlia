using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Cogito.Kademlia.Core;

namespace Cogito.Kademlia.Network.Protocol.Datagram
{

    /// <summary>
    /// Reads packet data from a sequence.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public static class KPacketReader<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Reads a series of <see cref="KIpEndpoint"/> records from the sequence.
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public static ReadOnlySpan<KIpEndpoint> ReadIpEndpoints(ref ReadOnlySequence<byte> sequence)
        {
            var l = sequence.AdvanceOver(sizeof(uint), BinaryPrimitives.ReadUInt32BigEndian);
            var v = sequence.AdvanceOver((int)l * Unsafe.SizeOf<KIpEndpoint>(), (ReadOnlyMemory<byte> m) => m);
            return MemoryMarshal.Cast<byte, KIpEndpoint>(v.Span);
        }

        public static ReadOnlySpan<KIpPeer<TKNodeId>> ReadIpPeer(ref ReadOnlySequence<byte> sequence)
        {

        }

        /// <summary>
        /// Reads the header from the sequence.
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public static KPacketHeader<TKNodeId> ReadHeader(ref ReadOnlySequence<byte> sequence)
        {
            KNodeId.Read<TKNodeId>(ref sequence, out var sender);
            var magic = sequence.AdvanceOver(sizeof(uint), BinaryPrimitives.ReadUInt32BigEndian);
            var type = sequence.AdvanceOver(sizeof(sbyte), _ => (KPacketType)_[0]);
            return new KPacketHeader<TKNodeId>(sender, magic, type);
        }

        /// <summary>
        /// Reads the PING request from the sequence.
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public static KPingRequestBody<TKNodeId> ReadPingRequest(ref ReadOnlySequence<byte> sequence)
        {
            var endpoints = ReadIpEndpoints(ref sequence);
            return new KPingRequestBody<TKNodeId>(endpoints);
        }

        /// <summary>
        /// Reads the PING response from the sequence.
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public static KPingResponseBody<TKNodeId> ReadPingResponse(ref ReadOnlySequence<byte> sequence)
        {
            var endpoints = ReadIpEndpoints(ref sequence);
            return new KPingResponseBody<TKNodeId>(endpoints);
        }

        /// <summary>
        /// Reads the STORE request from the sequence.
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public static KStoreRequestBody<TKNodeId> ReadStoreRequest(ref ReadOnlySequence<byte> sequence)
        {
            KNodeId.Read<TKNodeId>(ref sequence, out var key);
            var l = sequence.AdvanceOver(sizeof(uint), BinaryPrimitives.ReadUInt32BigEndian);
            var v = sequence.AdvanceOver((int)l, (ReadOnlyMemory<byte> m) => m);
            return new KStoreRequestBody<TKNodeId>(key, v.Span);
        }

        /// <summary>
        /// Reads the STORE response from the sequence.
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public static KStoreResponseBody<TKNodeId> ReadStoreResponse(ref ReadOnlySequence<byte> sequence)
        {
            KNodeId.Read<TKNodeId>(ref sequence, out var key);
            return new KStoreResponseBody<TKNodeId>(key);
        }

        /// <summary>
        /// Reads the FIND_NODE request from the sequence.
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public static KFindNodeRequestBody<TKNodeId> ReadFindNodeRequest(ref ReadOnlySequence<byte> sequence)
        {
            KNodeId.Read<TKNodeId>(ref sequence, out var nodeId);
            return new KFindNodeRequestBody<TKNodeId>(nodeId);
        }

        /// <summary>
        /// Reads the FIND_NODE response from the sequence.
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public static KFindNodeResponse<TKNodeId> ReadFindNodeResponse(ref ReadOnlySequence<byte> sequence)
        {
            KNodeId.Read<TKNodeId>(ref sequence, out var key);
            return new KFindNodeResponse<TKNodeId>(key);
        }

        /// <summary>
        /// Reads the FIND_VALUE request from the sequence.
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public static KFindValueRequestBody<TKNodeId> ReadFindValueRequest(ref ReadOnlySequence<byte> sequence)
        {
            KNodeId.Read<TKNodeId>(ref sequence, out var key);
            return new KFindValueRequestBody<TKNodeId>(key);
        }

        /// <summary>
        /// Reads the FIND_VALUE response from the sequence.
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public static KFindValueResponseBody<TKNodeId> ReadFindValueResponse(ref ReadOnlySequence<byte> sequence)
        {
            KNodeId.Read<TKNodeId>(ref sequence, out var key);
            var l = sequence.AdvanceOver(sizeof(uint), BinaryPrimitives.ReadUInt32BigEndian);
            var v = sequence.AdvanceOver((int)l, (ReadOnlyMemory<byte> m) => m);
            return new KFindValueResponseBody<TKNodeId>(key, v.Span);
        }

    }

}
