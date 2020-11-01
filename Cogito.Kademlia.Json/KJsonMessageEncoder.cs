using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text.Json;

using Cogito.Kademlia.Net;
using Cogito.Kademlia.Protocols;

namespace Cogito.Kademlia.Json
{

    /// <summary>
    /// Implements a <see cref="IKMessageEncoder{TKNodeId}"/> using JSON.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public class KJsonMessageEncoder<TKNodeId> : IKMessageEncoder<TKNodeId, IKIpProtocolResourceProvider<TKNodeId>>
        where TKNodeId : unmanaged
    {

        public void Encode(IKIpProtocolResourceProvider<TKNodeId> resources, IBufferWriter<byte> buffer, KMessageSequence<TKNodeId> sequence)
        {
            using var writer = new Utf8JsonWriter(buffer);
            writer.WriteStartObject();
            writer.WriteNumber("network", sequence.Network);
            writer.WritePropertyName("messages");
            Write(writer, resources, sequence);
            writer.WriteEndObject();
        }

        void Write(Utf8JsonWriter writer, IKIpProtocolResourceProvider<TKNodeId> resources, IEnumerable<IKMessage<TKNodeId>> messages)
        {
            writer.WriteStartArray();

            foreach (var message in messages)
                Write(writer, resources, message);

            writer.WriteEndArray();
        }

        void Write(Utf8JsonWriter writer, IKIpProtocolResourceProvider<TKNodeId> resources, IKMessage<TKNodeId> message)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("header");
            Write(writer, resources, message.Header);

            writer.WritePropertyName("type");

            switch (message.Body)
            {
                case KPingRequest<TKNodeId> request:
                    writer.WriteStringValue("PING");
                    writer.WritePropertyName("body");
                    Write(writer, resources, request);
                    break;
                case KPingResponse<TKNodeId> response:
                    writer.WriteStringValue("PING_RESPONSE");
                    writer.WritePropertyName("body");
                    Write(writer, resources, response);
                    break;
                case KStoreRequest<TKNodeId> request:
                    writer.WriteStringValue("STORE");
                    writer.WritePropertyName("body");
                    Write(writer, resources, request);
                    break;
                case KStoreResponse<TKNodeId> response:
                    writer.WriteStringValue("STORE_RESPONSE");
                    writer.WritePropertyName("body");
                    Write(writer, resources, response);
                    break;
                case KFindNodeRequest<TKNodeId> request:
                    writer.WriteStringValue("FIND_NODE");
                    writer.WritePropertyName("body");
                    Write(writer, resources, request);
                    break;
                case KFindNodeResponse<TKNodeId> response:
                    writer.WriteStringValue("FIND_NODE_RESPONSE");
                    writer.WritePropertyName("body");
                    Write(writer, resources, response);
                    break;
                case KFindValueRequest<TKNodeId> request:
                    writer.WriteStringValue("FIND_VALUE");
                    writer.WritePropertyName("body");
                    Write(writer, resources, request);
                    break;
                case KFindValueResponse<TKNodeId> response:
                    writer.WriteStringValue("FIND_VALUE_RESPONSE");
                    writer.WritePropertyName("body");
                    Write(writer, resources, response);
                    break;
                default:
                    throw new InvalidOperationException();
            }

            writer.WriteEndObject();
        }

        void Write(Utf8JsonWriter writer, IKIpProtocolResourceProvider<TKNodeId> resources, TKNodeId nodeId)
        {
#if NET47
            var a = new byte[KNodeId<TKNodeId>.SizeOf];
#else
            var a = (Span<byte>)stackalloc byte[KNodeId<TKNodeId>.SizeOf];
#endif
            nodeId.Write(a);
            writer.WriteBase64StringValue(a);
        }

        void Write(Utf8JsonWriter writer, IKIpProtocolResourceProvider<TKNodeId> resources, KMessageHeader<TKNodeId> header)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("sender");
            Write(writer, resources, header.Sender);
            writer.WriteNumber("magic", header.Magic);
            writer.WriteEndObject();
        }

        void Write(Utf8JsonWriter writer, IKIpProtocolResourceProvider<TKNodeId> resources, KPingRequest<TKNodeId> request)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("endpoints");
            Write(writer, resources, request.Endpoints);
            writer.WriteEndObject();
        }

        void Write(Utf8JsonWriter writer, IKIpProtocolResourceProvider<TKNodeId> resources, KPingResponse<TKNodeId> response)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("endpoints");
            Write(writer, resources, response.Endpoints);
            writer.WriteEndObject();
        }

        void Write(Utf8JsonWriter writer, IKIpProtocolResourceProvider<TKNodeId> resources, KStoreRequest<TKNodeId> request)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("key");
            Write(writer, resources, request.Key);

            writer.WriteString("mode", request.Mode switch
            {
                KStoreRequestMode.Primary => "PRIMARY",
                KStoreRequestMode.Replica => "REPLICA",
                _ => throw new InvalidOperationException(),
            });

            if (request.Value is KValueInfo value)
            {
                writer.WritePropertyName("value");
                Write(writer, resources, value);
            }
        }

        void Write(Utf8JsonWriter writer, IKIpProtocolResourceProvider<TKNodeId> resources, KStoreResponse<TKNodeId> response)
        {
            writer.WriteStartObject();
            writer.WriteString("status", response.Status switch
            {
                KStoreResponseStatus.Invalid => "INVALID",
                KStoreResponseStatus.Success => "SUCCESS",
                _ => throw new InvalidOperationException(),
            });
            writer.WriteEndObject();
        }

        void Write(Utf8JsonWriter writer, IKIpProtocolResourceProvider<TKNodeId> resources, KFindNodeRequest<TKNodeId> request)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("key");
            Write(writer, resources, request.Key);
            writer.WriteEndObject();
        }

        void Write(Utf8JsonWriter writer, IKIpProtocolResourceProvider<TKNodeId> resources, KFindNodeResponse<TKNodeId> response)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("peers");
            Write(writer, resources, response.Peers);
            writer.WriteEndObject();
        }

        void Write(Utf8JsonWriter writer, IKIpProtocolResourceProvider<TKNodeId> resources, KFindValueRequest<TKNodeId> request)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("key");
            Write(writer, resources, request.Key);
            writer.WriteEndObject();
        }

        void Write(Utf8JsonWriter writer, IKIpProtocolResourceProvider<TKNodeId> resources, KFindValueResponse<TKNodeId> response)
        {
            writer.WriteStartObject();

            if (response.Value is KValueInfo value)
            {
                writer.WritePropertyName("value");
                Write(writer, resources, value);
            }

            writer.WritePropertyName("peers");
            Write(writer, resources, response.Peers);
            writer.WriteEndObject();
        }

        void Write(Utf8JsonWriter writer, IKIpProtocolResourceProvider<TKNodeId> resources, KValueInfo value)
        {
            writer.WriteStartObject();
            writer.WriteBase64String("data", value.Data);
            writer.WriteNumber("version", value.Version);
            writer.WriteNumber("ttl", (value.Expiration - DateTime.UtcNow).TotalMilliseconds);
            writer.WriteEndObject();
        }

        void Write(Utf8JsonWriter writer, IKIpProtocolResourceProvider<TKNodeId> resources, KPeerEndpointInfo<TKNodeId>[] peers)
        {
            writer.WriteStartArray();

            foreach (var peer in peers)
                Write(writer, resources, peer);

            writer.WriteEndArray();
        }

        void Write(Utf8JsonWriter writer, IKIpProtocolResourceProvider<TKNodeId> resources, KPeerEndpointInfo<TKNodeId> peer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("id");
            Write(writer, resources, peer.Id);
            writer.WritePropertyName("endpoints");
            Write(writer, resources, peer.Endpoints);
            writer.WriteEndObject();
        }

        void Write(Utf8JsonWriter writer, IKIpProtocolResourceProvider<TKNodeId> resources, IEnumerable<IKEndpoint<TKNodeId>> endpoints)
        {
            writer.WriteStartArray();

            foreach (var endpoint in endpoints)
                Write(writer, resources, endpoint);

            writer.WriteEndArray();
        }

        void Write(Utf8JsonWriter writer, IKIpProtocolResourceProvider<TKNodeId> resources, IKEndpoint<TKNodeId> endpoint)
        {
            // we only support IP protocol endpoints
            var ip = endpoint as KIpProtocolEndpoint<TKNodeId>;
            if (ip is null)
                writer.WriteNullValue();

            writer.WriteStringValue(endpoint.ToString());
        }

    }

}
