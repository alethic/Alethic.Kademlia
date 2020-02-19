using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;

namespace Cogito.Kademlia.Network.Protocol.Datagram
{

    /// <summary>
    /// Writes request data to a buffer.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public static class KPacketWriter<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Writes the endpoint to the buffer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="endpoints"></param>
        public static void WriteIpEndpoint(IBufferWriter<byte> writer, in KIpEndpoint endpoint)
        {
            endpoint.Write(writer);
        }

        /// <summary>
        /// Writes the endpoint to the buffer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="endpoints"></param>
        public static void WriteIpEndpoints(IBufferWriter<byte> writer, IEnumerable<KIpEndpoint> endpoints)
        {
            BinaryPrimitives.WriteUInt32BigEndian(writer.GetMemory(sizeof(uint)).Span, (uint)endpoints.Count());
            writer.Advance(sizeof(uint));
            foreach (var endpoint in endpoints)
                WriteIpEndpoint(writer, endpoint);
        }

        /// <summary>
        /// Writes the header to the buffer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="header"></param>
        public static void WriteHeader(IBufferWriter<byte> writer, in KPacketHeader<TKNodeId> header)
        {
            // writer Sender
            header.Sender.Write(writer);

            // write Magic
            BinaryPrimitives.WriteUInt32BigEndian(writer.GetMemory(sizeof(uint)).Span, header.Magic);
            writer.Advance(sizeof(uint));

            // write Type
            writer.GetSpan(sizeof(byte))[0] = (byte)header.Type;
            writer.Advance(sizeof(byte));
        }

        /// <summary>
        /// Writes the PING request to the buffer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="body"></param>
        public static void WritePingRequest(IBufferWriter<byte> writer, in KPingRequestBody<TKNodeId> body)
        {
            WriteIpEndpoints(writer, body.Endpoints);
        }

        /// <summary>
        /// Writes the PING response to the buffer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="body"></param>
        public static void WritePingResponse(IBufferWriter<byte> writer, in KPingResponseBody<TKNodeId> body)
        {
            WriteIpEndpoints(writer, body.Endpoints);
        }

        /// <summary>
        /// Writes the STORE request to the buffer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="body"></param>
        public static void WriteStoreRequest(IBufferWriter<byte> writer, in KStoreRequestBody<TKNodeId> body)
        {
            body.Key.Write(writer);
            BinaryPrimitives.WriteUInt32BigEndian(writer.GetSpan(sizeof(uint)), (uint)body.Value.Length);
            writer.Advance(sizeof(uint));
            writer.Write(body.Value);
        }

        /// <summary>
        /// Writes the STORE request to the buffer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="body"></param>
        public static void WriteStoreResponse(IBufferWriter<byte> writer, in KStoreResponseBody<TKNodeId> body)
        {
            body.Key.Write(writer);
        }

        /// <summary>
        /// Writes the FIND_NODE request to the buffer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="body"></param>
        public static void WriteFindNodeRequest(IBufferWriter<byte> writer, in KFindNodeRequestBody<TKNodeId> body)
        {
            body.NodeId.Write(writer);
        }

        /// <summary>
        /// Writes the FIND_NODE request to the buffer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="body"></param>
        public static void WriteFindNodeResponse(IBufferWriter<byte> writer, in KFindNodeResponseBody<TKNodeId> body)
        {
            body.Key.Write(writer);
        }

        /// <summary>
        /// Writes the FIND_VALUE request to the buffer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="body"></param>
        public static void WriteFindValueRequest(IBufferWriter<byte> writer, in KFindValueRequestBody<TKNodeId> body)
        {
            body.Key.Write(writer);
        }

        /// <summary>
        /// Writes the FIND_VALUE response to the buffer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="body"></param>
        public static void WriteFindValueResponse(IBufferWriter<byte> writer, in KFindValueResponseBody<TKNodeId> body)
        {
            body.Key.Write(writer);
        }

    }

}
