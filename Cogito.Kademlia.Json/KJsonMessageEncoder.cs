using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text.Json;

namespace Cogito.Kademlia.Json
{

    /// <summary>
    /// Implements a <see cref="IKMessageEncoder{TNodeId}"/> using JSON.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public class KJsonMessageEncoder<TNodeId>
        where TNodeId : unmanaged
    {

        public void Encode(IKMessageContext<TNodeId> context, IBufferWriter<byte> buffer, KMessageSequence<TNodeId> sequence)
        {
            using var writer = new Utf8JsonWriter(buffer);
            writer.WriteStartObject();
            writer.WriteNumber("network", sequence.Network);
            writer.WritePropertyName("messages");
            Write(writer, context, sequence);
            writer.WriteEndObject();
        }

        void Write(Utf8JsonWriter writer, IKMessageContext<TNodeId> context, IEnumerable<IKMessage<TNodeId>> messages)
        {
            writer.WriteStartArray();

            foreach (var message in messages)
                Write(writer, context, message);

            writer.WriteEndArray();
        }

        void Write(Utf8JsonWriter writer, IKMessageContext<TNodeId> context, IKMessage<TNodeId> message)
        {
            switch (message)
            {
                case IKRequest<TNodeId> request:
                    Write(writer, context, request);
                    break;
                case IKResponse<TNodeId> response:
                    Write(writer, context, response);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        void Write(Utf8JsonWriter writer, IKMessageContext<TNodeId> context, IKRequest<TNodeId> message)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("header");
            Write(writer, context, message.Header);

            writer.WritePropertyName("type");

            switch (message)
            {
                case KRequest<TNodeId, KPingRequest<TNodeId>> request:
                    writer.WriteStringValue("PING");
                    writer.WritePropertyName("body");
                    Write(writer, context, request.Body.Value);
                    break;
                case KRequest<TNodeId, KStoreRequest<TNodeId>> request:
                    writer.WriteStringValue("STORE");
                    writer.WritePropertyName("body");
                    Write(writer, context, request.Body.Value);
                    break;
                case KRequest<TNodeId, KFindNodeRequest<TNodeId>> request:
                    writer.WriteStringValue("FIND_NODE");
                    writer.WritePropertyName("body");
                    Write(writer, context, request.Body.Value);
                    break;
                case KRequest<TNodeId, KFindValueRequest<TNodeId>> request:
                    writer.WriteStringValue("FIND_VALUE");
                    writer.WritePropertyName("body");
                    Write(writer, context, request.Body.Value);
                    break;
                default:
                    throw new InvalidOperationException();
            }

            writer.WriteEndObject();
        }

        void Write(Utf8JsonWriter writer, IKMessageContext<TNodeId> context, IKResponse<TNodeId> message)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("header");
            Write(writer, context, message.Header);

            writer.WritePropertyName("status");
            Write(writer, context, message.Status);

            writer.WritePropertyName("type");

            switch (message)
            {
                case KRequest<TNodeId, KPingRequest<TNodeId>> request:
                    writer.WriteStringValue("PING");
                    writer.WritePropertyName("body");
                    Write(writer, context, request.Body.Value);
                    break;
                case KResponse<TNodeId, KPingResponse<TNodeId>> response:
                    writer.WriteStringValue("PING_RESPONSE");
                    writer.WritePropertyName("body");
                    Write(writer, context, response.Body.Value);
                    break;
                case KRequest<TNodeId, KStoreRequest<TNodeId>> request:
                    writer.WriteStringValue("STORE");
                    writer.WritePropertyName("body");
                    Write(writer, context, request.Body.Value);
                    break;
                case KResponse<TNodeId, KStoreResponse<TNodeId>> response:
                    writer.WriteStringValue("STORE_RESPONSE");
                    writer.WritePropertyName("body");
                    Write(writer, context, response.Body.Value);
                    break;
                case KRequest<TNodeId, KFindNodeRequest<TNodeId>> request:
                    writer.WriteStringValue("FIND_NODE");
                    writer.WritePropertyName("body");
                    Write(writer, context, request.Body.Value);
                    break;
                case KResponse<TNodeId, KFindNodeResponse<TNodeId>> response:
                    writer.WriteStringValue("FIND_NODE_RESPONSE");
                    writer.WritePropertyName("body");
                    Write(writer, context, response.Body.Value);
                    break;
                case KRequest<TNodeId, KFindValueRequest<TNodeId>> request:
                    writer.WriteStringValue("FIND_VALUE");
                    writer.WritePropertyName("body");
                    Write(writer, context, request.Body.Value);
                    break;
                case KResponse<TNodeId, KFindValueResponse<TNodeId>> response:
                    writer.WriteStringValue("FIND_VALUE_RESPONSE");
                    writer.WritePropertyName("body");
                    Write(writer, context, response.Body.Value);
                    break;
                default:
                    throw new InvalidOperationException();
            }

            writer.WriteEndObject();
        }

        void Write(Utf8JsonWriter writer, IKMessageContext<TNodeId> context, TNodeId nodeId)
        {
            var a = (Span<byte>)stackalloc byte[KNodeId<TNodeId>.SizeOf];
            nodeId.Write(a);
            writer.WriteBase64StringValue(a);
        }

        void Write(Utf8JsonWriter writer, IKMessageContext<TNodeId> context, KResponseStatus status)
        {
            writer.WriteStringValue(status switch
            {
                KResponseStatus.Success => "SUCCESS",
                KResponseStatus.Failure => "FAILURE",
                _ => throw new InvalidOperationException(),
            });
        }

        void Write(Utf8JsonWriter writer, IKMessageContext<TNodeId> context, KMessageHeader<TNodeId> header)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("sender");
            Write(writer, context, header.Sender);
            writer.WriteNumber("replyId", header.ReplyId);
            writer.WriteEndObject();
        }

        void Write(Utf8JsonWriter writer, IKMessageContext<TNodeId> context, KPingRequest<TNodeId> request)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("endpoints");
            Write(writer, context, request.Endpoints);
            writer.WriteEndObject();
        }

        void Write(Utf8JsonWriter writer, IKMessageContext<TNodeId> context, KPingResponse<TNodeId> response)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("endpoints");
            Write(writer, context, response.Endpoints);
            writer.WriteEndObject();
        }

        void Write(Utf8JsonWriter writer, IKMessageContext<TNodeId> context, KStoreRequest<TNodeId> request)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("key");
            Write(writer, context, request.Key);

            writer.WriteString("mode", request.Mode switch
            {
                KStoreRequestMode.Primary => "PRIMARY",
                KStoreRequestMode.Replica => "REPLICA",
                _ => throw new InvalidOperationException(),
            });

            if (request.Value is KValueInfo value)
            {
                writer.WritePropertyName("value");
                Write(writer, context, value);
            }
        }

        void Write(Utf8JsonWriter writer, IKMessageContext<TNodeId> context, KStoreResponse<TNodeId> response)
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

        void Write(Utf8JsonWriter writer, IKMessageContext<TNodeId> context, KFindNodeRequest<TNodeId> request)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("key");
            Write(writer, context, request.Key);
            writer.WriteEndObject();
        }

        void Write(Utf8JsonWriter writer, IKMessageContext<TNodeId> context, KFindNodeResponse<TNodeId> response)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("peers");
            Write(writer, context, response.Peers);
            writer.WriteEndObject();
        }

        void Write(Utf8JsonWriter writer, IKMessageContext<TNodeId> context, KFindValueRequest<TNodeId> request)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("key");
            Write(writer, context, request.Key);
            writer.WriteEndObject();
        }

        void Write(Utf8JsonWriter writer, IKMessageContext<TNodeId> context, KFindValueResponse<TNodeId> response)
        {
            writer.WriteStartObject();

            if (response.Value is KValueInfo value)
            {
                writer.WritePropertyName("value");
                Write(writer, context, value);
            }

            writer.WritePropertyName("peers");
            Write(writer, context, response.Peers);
            writer.WriteEndObject();
        }

        void Write(Utf8JsonWriter writer, IKMessageContext<TNodeId> context, KValueInfo value)
        {
            writer.WriteStartObject();
            writer.WriteBase64String("data", value.Data);
            writer.WriteNumber("version", value.Version);
            writer.WriteNumber("ttl", (value.Expiration - DateTime.UtcNow).TotalMilliseconds);
            writer.WriteEndObject();
        }

        void Write(Utf8JsonWriter writer, IKMessageContext<TNodeId> context, KPeerInfo<TNodeId>[] peers)
        {
            writer.WriteStartArray();

            foreach (var peer in peers)
                Write(writer, context, peer);

            writer.WriteEndArray();
        }

        void Write(Utf8JsonWriter writer, IKMessageContext<TNodeId> context, KPeerInfo<TNodeId> peer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("id");
            Write(writer, context, peer.Id);
            writer.WritePropertyName("endpoints");
            Write(writer, context, peer.Endpoints);
            writer.WriteEndObject();
        }

        void Write(Utf8JsonWriter writer, IKMessageContext<TNodeId> context, IEnumerable<IKProtocolEndpoint<TNodeId>> endpoints)
        {
            writer.WriteStartArray();

            foreach (var endpoint in endpoints)
                Write(writer, context, endpoint);

            writer.WriteEndArray();
        }

        void Write(Utf8JsonWriter writer, IKMessageContext<TNodeId> context, IKProtocolEndpoint<TNodeId> endpoint)
        {
            writer.WriteStringValue(endpoint?.ToUri()?.ToString());
        }

    }

}
