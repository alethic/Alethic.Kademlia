using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Cogito.Kademlia.Json
{

    /// <summary>
    /// Implements a <see cref="IKMessageDecoder{TNodeId}"/> using JSON.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public class KJsonMessageDecoder<TNodeId>
        where TNodeId : unmanaged
    {

        public KMessageSequence<TNodeId> Decode(IKMessageContext<TNodeId> context, ReadOnlySequence<byte> buffer)
        {
            return DecodeMessageSequence(context, JsonDocument.Parse(buffer).RootElement);
        }

        KMessageSequence<TNodeId> DecodeMessageSequence(IKMessageContext<TNodeId> context, JsonElement element)
        {
            return new KMessageSequence<TNodeId>(element.GetProperty("network").GetUInt64(), DecodeMessages(context, element.GetProperty("messages")));
        }

        IEnumerable<IKMessage<TNodeId>> DecodeMessages(IKMessageContext<TNodeId> context, JsonElement element)
        {
            foreach (var message in element.EnumerateArray())
                yield return DecodeMessage(context, message);
        }

        IKMessage<TNodeId> DecodeMessage(IKMessageContext<TNodeId> context, JsonElement message)
        {
            return (message.GetProperty("type").GetString()) switch
            {
                "PING" => CreateRequest(context, DecodeMessageHeader(context, message.GetProperty("header")), DecodePingRequest(context, message.GetProperty("body"))),
                "PING_RESPONSE" => CreateResponse(context, DecodeMessageHeader(context, message.GetProperty("header")), DecodeResponseStatus(context, message.GetProperty("status")), DecodePingResponse(context, message.GetProperty("body"))),
                "STORE" => CreateRequest(context, DecodeMessageHeader(context, message.GetProperty("header")), DecodeStoreRequest(context, message.GetProperty("body"))),
                "STORE_RESPONSE" => CreateResponse(context, DecodeMessageHeader(context, message.GetProperty("header")), DecodeResponseStatus(context, message.GetProperty("status")), DecodeStoreResponse(context, message.GetProperty("body"))),
                "FIND_NODE" => CreateRequest(context, DecodeMessageHeader(context, message.GetProperty("header")), DecodeFindNodeRequest(context, message.GetProperty("body"))),
                "FIND_NODE_RESPONSE" => CreateResponse(context, DecodeMessageHeader(context, message.GetProperty("header")), DecodeResponseStatus(context, message.GetProperty("status")), DecodeFindNodeResponse(context, message.GetProperty("body"))),
                "FIND_VALUE" => CreateRequest(context, DecodeMessageHeader(context, message.GetProperty("header")), DecodeFindValueRequest(context, message.GetProperty("body"))),
                "FIND_VALUE_RESPONSE" => CreateResponse(context, DecodeMessageHeader(context, message.GetProperty("header")), DecodeResponseStatus(context, message.GetProperty("status")), DecodeFindValueResponse(context, message.GetProperty("body"))),
                _ => throw new InvalidOperationException(),
            };
        }

        KResponseStatus DecodeResponseStatus(IKMessageContext<TNodeId> context, JsonElement element)
        {
            return element.GetString() switch
            {
                "SUCCESS" => KResponseStatus.Success,
                "FAILURE" => KResponseStatus.Failure,
                _ => throw new InvalidOperationException(),
            };
        }

        /// <summary>
        /// Creates a message from the components.
        /// </summary>
        /// <typeparam name="TBody"></typeparam>
        /// <param name="context"></param>
        /// <param name="header"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        IKRequest<TNodeId> CreateRequest<TBody>(IKMessageContext<TNodeId> context, KMessageHeader<TNodeId> header, TBody? body)
            where TBody : struct, IKRequestBody<TNodeId>
        {
            return new KRequest<TNodeId, TBody>(header, body);
        }

        /// <summary>
        /// Creates a message from the components.
        /// </summary>
        /// <typeparam name="TBody"></typeparam>
        /// <param name="context"></param>
        /// <param name="header"></param>
        /// <param name="status"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        IKResponse<TNodeId> CreateResponse<TBody>(IKMessageContext<TNodeId> context, KMessageHeader<TNodeId> header, KResponseStatus status, TBody? body)
            where TBody : struct, IKResponseBody<TNodeId>
        {
            return new KResponse<TNodeId, TBody>(header, status, body);
        }

        /// <summary>
        /// Decodes a <typeparamref name="TNodeId"/>.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        TNodeId DecodeNodeId(IKMessageContext<TNodeId> context, JsonElement element)
        {
            return DecodeNodeId(context, element.GetBytesFromBase64());
        }

        /// <summary>
        /// Decodes a <typeparamref name="TNodeId"/>.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        TNodeId DecodeNodeId(IKMessageContext<TNodeId> context, byte[] bytes)
        {
#if NET47
            return KNodeId<TNodeId>.Read(bytes);
#else
            return KNodeId<TNodeId>.Read(bytes.AsSpan());
#endif
        }

        /// <summary>
        /// Decodes a message header.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        KMessageHeader<TNodeId> DecodeMessageHeader(IKMessageContext<TNodeId> context, JsonElement element)
        {
            return new KMessageHeader<TNodeId>(DecodeNodeId(context, element.GetProperty("sender")), element.GetProperty("replyId").GetUInt32());
        }

        /// <summary>
        /// Decodes a PING request.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        KPingRequest<TNodeId>? DecodePingRequest(IKMessageContext<TNodeId> context, JsonElement element)
        {
            return element.ValueKind != JsonValueKind.Undefined ? new KPingRequest<TNodeId>(element.GetProperty("endpoints").EnumerateArray().Select(i => new Uri(i.GetString()))) : (KPingRequest<TNodeId>?)null;
        }

        /// <summary>
        /// Decodes a PING response.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        KPingResponse<TNodeId>? DecodePingResponse(IKMessageContext<TNodeId> context, JsonElement element)
        {
            return element.ValueKind != JsonValueKind.Undefined ? new KPingResponse<TNodeId>(element.GetProperty("endpoints").EnumerateArray().Select(i => new Uri(i.GetString()))) : (KPingResponse<TNodeId>?)null;
        }

        /// <summary>
        /// Decodes a STORE request.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        KStoreRequest<TNodeId>? DecodeStoreRequest(IKMessageContext<TNodeId> context, JsonElement element)
        {
            return element.ValueKind != JsonValueKind.Undefined ? new KStoreRequest<TNodeId>(
                DecodeNodeId(context, element.GetProperty("key")),
                DecodeStoreRequestMode(context, element.GetProperty("mode")),
                element.TryGetProperty("value", out var value) ?
                    new KValueInfo(
                        value.GetProperty("data").GetBytesFromBase64(),
                        value.GetProperty("version").GetUInt64(),
                        DateTime.UtcNow + TimeSpan.FromSeconds(value.GetProperty("ttl").GetInt32())) :
                    (KValueInfo?)null) : (KStoreRequest<TNodeId>?)null;
        }

        /// <summary>
        /// Decodes a <see cref="StoreRequest.Types.StoreRequestMode" />.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        KStoreRequestMode DecodeStoreRequestMode(IKMessageContext<TNodeId> context, JsonElement element)
        {
            return element.GetString() switch
            {
                "PRIMARY" => KStoreRequestMode.Primary,
                "REPLICA" => KStoreRequestMode.Replica,
                _ => throw new InvalidOperationException(),
            };
        }

        /// <summary>
        /// Decodes a STORE response.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        KStoreResponse<TNodeId>? DecodeStoreResponse(IKMessageContext<TNodeId> context, JsonElement element)
        {
            return element.ValueKind != JsonValueKind.Undefined ? new KStoreResponse<TNodeId>(DecodeStoreResponseStatus(context, element.GetProperty("status"))) : (KStoreResponse<TNodeId>?)null;
        }

        /// <summary>
        /// Decodes the STORE response status.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        KStoreResponseStatus DecodeStoreResponseStatus(IKMessageContext<TNodeId> context, JsonElement status)
        {
            return status.GetString() switch
            {
                "INVALID" => KStoreResponseStatus.Invalid,
                "SUCCESS" => KStoreResponseStatus.Success,
                _ => throw new InvalidOperationException(),
            };
        }

        /// <summary>
        /// Decodes a FIND_NODE request.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        KFindNodeRequest<TNodeId>? DecodeFindNodeRequest(IKMessageContext<TNodeId> context, JsonElement element)
        {
            return element.ValueKind != JsonValueKind.Undefined ? new KFindNodeRequest<TNodeId>(DecodeNodeId(context, element.GetProperty("key"))) : (KFindNodeRequest<TNodeId>?)null;
        }

        /// <summary>
        /// Decodes a FIND_NODE response.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        KFindNodeResponse<TNodeId>? DecodeFindNodeResponse(IKMessageContext<TNodeId> context, JsonElement element)
        {
            return element.ValueKind != JsonValueKind.Undefined ? new KFindNodeResponse<TNodeId>(DecodeNodes(context, element.GetProperty("nodes")).ToArray()) : (KFindNodeResponse<TNodeId>?)null;
        }

        /// <summary>
        /// Decodes a FIND_NODE request.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        KFindValueRequest<TNodeId>? DecodeFindValueRequest(IKMessageContext<TNodeId> context, JsonElement element)
        {
            return element.ValueKind != JsonValueKind.Undefined ? new KFindValueRequest<TNodeId>(DecodeNodeId(context, element.GetProperty("key"))) : (KFindValueRequest<TNodeId>?)null;
        }

        /// <summary>
        /// Decodes a FIND_NODE response.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        KFindValueResponse<TNodeId>? DecodeFindValueResponse(IKMessageContext<TNodeId> context, JsonElement element)
        {
            return element.ValueKind != JsonValueKind.Undefined ? new KFindValueResponse<TNodeId>(
                DecodeNodes(context, element.GetProperty("nodes")).ToArray(),
                element.TryGetProperty("value", out var value) ?
                    new KValueInfo(
                        value.GetProperty("data").GetBytesFromBase64(),
                        value.GetProperty("version").GetUInt64(),
                        DateTime.UtcNow + TimeSpan.FromSeconds(value.GetProperty("ttl").GetInt32())) :
                    (KValueInfo?)null) : (KFindValueResponse<TNodeId>?)null;
        }

        /// <summary>
        /// Decodes a list of nodes.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        IEnumerable<KNodeInfo<TNodeId>> DecodeNodes(IKMessageContext<TNodeId> context, JsonElement element)
        {
            foreach (var node in element.EnumerateArray())
                yield return DecodeNode(context, node);
        }

        /// <summary>
        /// Decodes a single peer.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        KNodeInfo<TNodeId> DecodeNode(IKMessageContext<TNodeId> context, JsonElement element)
        {
            return new KNodeInfo<TNodeId>(DecodeNodeId(context, element.GetProperty("id")), element.GetProperty("endpoints").EnumerateArray().Select(i => new Uri(i.GetString())));
        }

    }

}
