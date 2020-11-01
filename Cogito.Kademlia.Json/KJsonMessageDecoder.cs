using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;

using Cogito.Kademlia.Net;
using Cogito.Kademlia.Protocols;

namespace Cogito.Kademlia.Json
{

    /// <summary>
    /// Implements a <see cref="IKMessageDecoder{TKNodeId}"/> using JSON.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public class KJsonMessageDecoder<TKNodeId> : IKMessageDecoder<TKNodeId, IKIpProtocolResourceProvider<TKNodeId>>
        where TKNodeId : unmanaged
    {

        public KMessageSequence<TKNodeId> Decode(IKIpProtocolResourceProvider<TKNodeId> resources, ReadOnlySequence<byte> buffer)
        {
            return DecodeMessageSequence(resources, JsonDocument.Parse(buffer).RootElement);
        }

        KMessageSequence<TKNodeId> DecodeMessageSequence(IKIpProtocolResourceProvider<TKNodeId> resources, JsonElement element)
        {
            return new KMessageSequence<TKNodeId>(element.GetProperty("network").GetUInt64(), DecodeMessages(resources, element.GetProperty("messages")));
        }

        IEnumerable<IKMessage<TKNodeId>> DecodeMessages(IKIpProtocolResourceProvider<TKNodeId> resources, JsonElement element)
        {
            foreach (var message in element.EnumerateArray())
                yield return DecodeMessage(resources, message);
        }

        IKMessage<TKNodeId> DecodeMessage(IKIpProtocolResourceProvider<TKNodeId> resources, JsonElement message)
        {
            switch (message.GetProperty("type").GetString())
            {
                case "PING":
                    return Create(resources, DecodeMessageHeader(resources, message.GetProperty("header")), DecodePingRequest(resources, message.GetProperty("body")));
                case "PING_RESPONSE":
                    return Create(resources, DecodeMessageHeader(resources, message.GetProperty("header")), DecodePingResponse(resources, message.GetProperty("body")));
                case "STORE":
                    return Create(resources, DecodeMessageHeader(resources, message.GetProperty("header")), DecodeStoreRequest(resources, message.GetProperty("body")));
                case "STORE_RESPONSE":
                    return Create(resources, DecodeMessageHeader(resources, message.GetProperty("header")), DecodeStoreResponse(resources, message.GetProperty("body")));
                case "FIND_NODE":
                    return Create(resources, DecodeMessageHeader(resources, message.GetProperty("header")), DecodeFindNodeRequest(resources, message.GetProperty("body")));
                case "FIND_NODE_RESPONSE":
                    return Create(resources, DecodeMessageHeader(resources, message.GetProperty("header")), DecodeFindNodeResponse(resources, message.GetProperty("body")));
                case "FIND_VALUE":
                    return Create(resources, DecodeMessageHeader(resources, message.GetProperty("header")), DecodeFindValueRequest(resources, message.GetProperty("body")));
                case "FIND_VALUE_RESPONSE":
                    return Create(resources, DecodeMessageHeader(resources, message.GetProperty("header")), DecodeFindValueResponse(resources, message.GetProperty("body")));
                default:
                    throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Creates a message from the components.
        /// </summary>
        /// <typeparam name="TBody"></typeparam>
        /// <param name="resources"></param>
        /// <param name="header"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        IKMessage<TKNodeId> Create<TBody>(IKIpProtocolResourceProvider<TKNodeId> resources, KMessageHeader<TKNodeId> header, TBody body)
            where TBody : struct, IKMessageBody<TKNodeId>
        {
            return new KMessage<TKNodeId, TBody>(header, body);
        }

        /// <summary>
        /// Decodes a <typeparamref name="TKNodeId"/>.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        TKNodeId DecodeNodeId(IKIpProtocolResourceProvider<TKNodeId> resources, JsonElement element)
        {
            return DecodeNodeId(resources, element.GetBytesFromBase64());
        }

        /// <summary>
        /// Decodes a <typeparamref name="TKNodeId"/>.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        TKNodeId DecodeNodeId(IKIpProtocolResourceProvider<TKNodeId> resources, byte[] bytes)
        {
#if NET47
            return KNodeId<TKNodeId>.Read(bytes);
#else
            return KNodeId<TKNodeId>.Read(bytes.AsSpan());
#endif
        }

        /// <summary>
        /// Decodes a message header.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        KMessageHeader<TKNodeId> DecodeMessageHeader(IKIpProtocolResourceProvider<TKNodeId> resources, JsonElement element)
        {
            return new KMessageHeader<TKNodeId>(DecodeNodeId(resources, element.GetProperty("sender")), element.GetProperty("magic").GetUInt64());
        }

        /// <summary>
        /// Decodes a PING request.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        KPingRequest<TKNodeId> DecodePingRequest(IKIpProtocolResourceProvider<TKNodeId> resources, JsonElement element)
        {
            return new KPingRequest<TKNodeId>(DecodeEndpoints(resources, element.GetProperty("endpoints")).ToArray());
        }

        /// <summary>
        /// Decodes a PING response.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        KPingResponse<TKNodeId> DecodePingResponse(IKIpProtocolResourceProvider<TKNodeId> resources, JsonElement element)
        {
            return new KPingResponse<TKNodeId>(DecodeEndpoints(resources, element.GetProperty("endpoints")).ToArray());
        }

        /// <summary>
        /// Decodes a STORE request.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        KStoreRequest<TKNodeId> DecodeStoreRequest(IKIpProtocolResourceProvider<TKNodeId> resources, JsonElement element)
        {
            return new KStoreRequest<TKNodeId>(
                DecodeNodeId(resources, element.GetProperty("key")),
                DecodeStoreRequestMode(resources, element.GetProperty("mode")),
                element.TryGetProperty("value", out var value) ?
                    new KValueInfo(
                        value.GetProperty("data").GetBytesFromBase64(),
                        value.GetProperty("version").GetUInt64(),
                        DateTime.UtcNow + TimeSpan.FromSeconds(value.GetProperty("ttl").GetDouble())) :
                    (KValueInfo?)null);
        }

        /// <summary>
        /// Decodes a <see cref="StoreRequest.Types.StoreRequestMode" />.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        KStoreRequestMode DecodeStoreRequestMode(IKIpProtocolResourceProvider<TKNodeId> resources, JsonElement element)
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
        /// <param name="resources"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        KStoreResponse<TKNodeId> DecodeStoreResponse(IKIpProtocolResourceProvider<TKNodeId> resources, JsonElement element)
        {
            return new KStoreResponse<TKNodeId>(DecodeStoreResponseStatus(resources, element.GetProperty("status")));
        }

        /// <summary>
        /// Decodes the STORE response status.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        KStoreResponseStatus DecodeStoreResponseStatus(IKIpProtocolResourceProvider<TKNodeId> resources, JsonElement status)
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
        /// <param name="resources"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        KFindNodeRequest<TKNodeId> DecodeFindNodeRequest(IKIpProtocolResourceProvider<TKNodeId> resources, JsonElement element)
        {
            return new KFindNodeRequest<TKNodeId>(DecodeNodeId(resources, element.GetProperty("key")));
        }

        /// <summary>
        /// Decodes a FIND_NODE response.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        KFindNodeResponse<TKNodeId> DecodeFindNodeResponse(IKIpProtocolResourceProvider<TKNodeId> resources, JsonElement element)
        {
            return new KFindNodeResponse<TKNodeId>(DecodePeers(resources, element.GetProperty("peers")).ToArray());
        }

        /// <summary>
        /// Decodes a FIND_NODE request.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        KFindValueRequest<TKNodeId> DecodeFindValueRequest(IKIpProtocolResourceProvider<TKNodeId> resources, JsonElement element)
        {
            return new KFindValueRequest<TKNodeId>(DecodeNodeId(resources, element.GetProperty("key")));
        }

        /// <summary>
        /// Decodes a FIND_NODE response.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        KFindValueResponse<TKNodeId> DecodeFindValueResponse(IKIpProtocolResourceProvider<TKNodeId> resources, JsonElement element)
        {
            return new KFindValueResponse<TKNodeId>(
                DecodePeers(resources, element.GetProperty("peers")).ToArray(),
                element.TryGetProperty("value", out var value) ?
                    new KValueInfo(
                        value.GetProperty("data").GetBytesFromBase64(),
                        value.GetProperty("version").GetUInt64(),
                        DateTime.UtcNow + TimeSpan.FromSeconds(value.GetProperty("ttl").GetDouble())) :
                    (KValueInfo?)null);
        }

        /// <summary>
        /// Decodes a list of peers.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        IEnumerable<KPeerEndpointInfo<TKNodeId>> DecodePeers(IKIpProtocolResourceProvider<TKNodeId> resources, JsonElement element)
        {
            foreach (var peer in element.EnumerateArray())
                yield return DecodePeer(resources, peer);
        }

        /// <summary>
        /// Decodes a single peer.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        KPeerEndpointInfo<TKNodeId> DecodePeer(IKIpProtocolResourceProvider<TKNodeId> resources, JsonElement element)
        {
            return new KPeerEndpointInfo<TKNodeId>(DecodeNodeId(resources, element.GetProperty("id")), new KEndpointSet<TKNodeId>(DecodeEndpoints(resources, element.GetProperty("endpoints"))));
        }

        /// <summary>
        /// Decodes a list of endpoints.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        IEnumerable<IKEndpoint<TKNodeId>> DecodeEndpoints(IKIpProtocolResourceProvider<TKNodeId> resources, JsonElement element)
        {
            foreach (var endpoint in element.EnumerateArray())
                yield return DecodeEndpoint(resources, endpoint);
        }

        /// <summary>
        /// Decodes a single endpoint.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        IKEndpoint<TKNodeId> DecodeEndpoint(IKIpProtocolResourceProvider<TKNodeId> resources, JsonElement element)
        {
            return DecodeEndpoint(resources, element.GetString());
        }

        /// <summary>
        /// Decodes a single endpoint.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        IKEndpoint<TKNodeId> DecodeEndpoint(IKIpProtocolResourceProvider<TKNodeId> resources, string value)
        {
#if NETCOREAPP3_0
            return resources.CreateEndpoint(new KIpEndpoint(IPEndPoint.Parse(value)));
#else
            return resources.CreateEndpoint(ParseIpEndpoint(value));
#endif
        }

        /// <summary>
        /// Slower parsing version.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        KIpEndpoint ParseIpEndpoint(string value)
        {
            if (Uri.TryCreate(value, UriKind.Absolute, out Uri uri))
                return new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port < 0 ? 0 : uri.Port);

            if (Uri.TryCreate(string.Concat("tcp://", value), UriKind.Absolute, out uri))
                return new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port < 0 ? 0 : uri.Port);

            if (Uri.TryCreate(string.Concat("tcp://", string.Concat("[", value, "]")), UriKind.Absolute, out uri))
                return new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port < 0 ? 0 : uri.Port);

            throw new FormatException("Failed to parse text to IPEndPoint");
        }

    }

}
