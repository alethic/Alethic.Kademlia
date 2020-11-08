using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

using Cogito.Kademlia.Network;

using Google.Protobuf;
using Google.Protobuf.Collections;

namespace Cogito.Kademlia.Protobuf
{

    /// <summary>
    /// Implements a <see cref="IKMessageDecoder{TNodeId}"/> using Google's Protocol Buffers.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    class KProtobufMessageDecoder<TNodeId>
        where TNodeId : unmanaged
    {

        public KMessageSequence<TNodeId> Decode(IKMessageContext<TNodeId> resources, ReadOnlySequence<byte> buffer)
        {
            var p = MessageSequence.Parser.ParseFrom(buffer);
            var s = new KMessageSequence<TNodeId>(p.Network, Decode(resources, p.Messages));
            return s;
        }

        IEnumerable<IKMessage<TNodeId>> Decode(IKMessageContext<TNodeId> resources, RepeatedField<Message> messages)
        {
            foreach (var message in messages)
                yield return Decode(resources, message);
        }

        IKMessage<TNodeId> Decode(IKMessageContext<TNodeId> context, Message message)
        {
            return message.MessageCase switch
            {
                Message.MessageOneofCase.Request => Decode(context, message.Request),
                Message.MessageOneofCase.Response => Decode(context, message.Response),
                _ => throw new InvalidOperationException(),
            };
        }

        IKRequest<TNodeId> Decode(IKMessageContext<TNodeId> resources, Request request)
        {
            return request.BodyCase switch
            {
                Request.BodyOneofCase.PingRequest => CreateRequest(resources, Decode(resources, request.Header), Decode(resources, request.PingRequest)),
                Request.BodyOneofCase.StoreRequest => CreateRequest(resources, Decode(resources, request.Header), Decode(resources, request.StoreRequest)),
                Request.BodyOneofCase.FindNodeRequest => CreateRequest(resources, Decode(resources, request.Header), Decode(resources, request.FindNodeRequest)),
                Request.BodyOneofCase.FindValueRequest => CreateRequest(resources, Decode(resources, request.Header), Decode(resources, request.FindValueRequest)),
                _ => throw new InvalidOperationException(),
            };
        }

        IKResponse<TNodeId> Decode(IKMessageContext<TNodeId> resources, Response response)
        {
            return response.BodyCase switch
            {
                Response.BodyOneofCase.PingResponse => CreateResponse(resources, Decode(resources, response.Header), Decode(resources, response.PingResponse)),
                Response.BodyOneofCase.StoreResponse => CreateResponse(resources, Decode(resources, response.Header), Decode(resources, response.StoreResponse)),
                Response.BodyOneofCase.FindNodeResponse => CreateResponse(resources, Decode(resources, response.Header), Decode(resources, response.FindNodeResponse)),
                Response.BodyOneofCase.FindValueResponse => CreateResponse(resources, Decode(resources, response.Header), Decode(resources, response.FindValueResponse)),
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
        IKRequest<TNodeId> CreateRequest<TBody>(IKMessageContext<TNodeId> context, KMessageHeader<TNodeId> header, TBody body)
            where TBody : struct, IKRequestBody<TNodeId>
        {
            return new KRequest<TNodeId, TBody>(header, body);
        }

        /// <summary>
        /// Creates a message from the components.
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="resources"></param>
        /// <param name="header"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        IKResponse<TNodeId> CreateResponse<TResponse>(IKMessageContext<TNodeId> resources, KMessageHeader<TNodeId> header, TResponse body)
            where TResponse : struct, IKResponseBody<TNodeId>
        {
            return new KResponse<TNodeId, TResponse>(header, KResponseStatus.Success, body);
        }

        /// <summary>
        /// Creates a message from the components.
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="resources"></param>
        /// <param name="header"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        IKResponse<TNodeId> CreateResponse<TResponse>(IKMessageContext<TNodeId> resources, KMessageHeader<TNodeId> header, Exception error)
            where TResponse : struct, IKResponseBody<TNodeId>
        {
            return new KResponse<TNodeId, TResponse>(header, KResponseStatus.Failure, null);
        }

        /// <summary>
        /// Decodes a <typeparamref name="TNodeId"/>.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        TNodeId DecodeNodeId(IKMessageContext<TNodeId> resources, ByteString bytes)
        {
#if NET47
            return KNodeId<TNodeId>.Read(bytes.ToByteArray());
#else
            return KNodeId<TNodeId>.Read(bytes.Span);
#endif
        }

        /// <summary>
        /// Decodes a message header.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        KMessageHeader<TNodeId> Decode(IKMessageContext<TNodeId> resources, Header header)
        {
            return new KMessageHeader<TNodeId>(DecodeNodeId(resources, header.Sender), header.ReplyId);
        }

        /// <summary>
        /// Decodes a PING request.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        KPingRequest<TNodeId> Decode(IKMessageContext<TNodeId> resources, PingRequest request)
        {
            return new KPingRequest<TNodeId>(request.Endpoints.Select(i => new Uri(i)));
        }

        /// <summary>
        /// Decodes a PING response.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        KPingResponse<TNodeId> Decode(IKMessageContext<TNodeId> resources, PingResponse response)
        {
            return new KPingResponse<TNodeId>(response.Endpoints.Select(i => new Uri(i)));
        }

        /// <summary>
        /// Decodes a STORE request.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        KStoreRequest<TNodeId> Decode(IKMessageContext<TNodeId> resources, StoreRequest request)
        {
            return new KStoreRequest<TNodeId>(
                DecodeNodeId(resources, request.Key),
                Decode(resources, request.Mode),
                request.HasValue ?
                    new KValueInfo(
                        request.Value.Data.ToByteArray(),
                        request.Value.Version,
                        DateTime.UtcNow + request.Value.Ttl.ToTimeSpan()) :
                    (KValueInfo?)null);
        }

        /// <summary>
        /// Decodes a <see cref="StoreRequest.Types.StoreRequestMode" />.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        KStoreRequestMode Decode(IKMessageContext<TNodeId> resources, StoreRequest.Types.StoreRequestMode mode)
        {
            return mode switch
            {
                StoreRequest.Types.StoreRequestMode.Primary => KStoreRequestMode.Primary,
                StoreRequest.Types.StoreRequestMode.Replica => KStoreRequestMode.Replica,
                _ => throw new InvalidOperationException(),
            };
        }

        /// <summary>
        /// Decodes a STORE response.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        KStoreResponse<TNodeId> Decode(IKMessageContext<TNodeId> resources, StoreResponse response)
        {
            return new KStoreResponse<TNodeId>(Decode(resources, response.Status));
        }

        /// <summary>
        /// Decodes the STORE response status.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        KStoreResponseStatus Decode(IKMessageContext<TNodeId> resources, StoreResponse.Types.StoreResponseStatus status)
        {
            return status switch
            {
                StoreResponse.Types.StoreResponseStatus.Invalid => KStoreResponseStatus.Invalid,
                StoreResponse.Types.StoreResponseStatus.Success => KStoreResponseStatus.Success,
                _ => throw new InvalidOperationException(),
            };
        }

        /// <summary>
        /// Decodes a FIND_NODE request.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        KFindNodeRequest<TNodeId> Decode(IKMessageContext<TNodeId> resources, FindNodeRequest request)
        {
            return new KFindNodeRequest<TNodeId>(DecodeNodeId(resources, request.Key));
        }

        /// <summary>
        /// Decodes a FIND_NODE response.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        KFindNodeResponse<TNodeId> Decode(IKMessageContext<TNodeId> resources, FindNodeResponse response)
        {
            return new KFindNodeResponse<TNodeId>(Decode(resources, response.Nodes).ToArray());
        }

        /// <summary>
        /// Decodes a FIND_NODE request.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        KFindValueRequest<TNodeId> Decode(IKMessageContext<TNodeId> resources, FindValueRequest request)
        {
            return new KFindValueRequest<TNodeId>(DecodeNodeId(resources, request.Key));
        }

        /// <summary>
        /// Decodes a FIND_NODE response.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        KFindValueResponse<TNodeId> Decode(IKMessageContext<TNodeId> resources, FindValueResponse response)
        {
            return new KFindValueResponse<TNodeId>(
                Decode(resources, response.Nodes).ToArray(),
                response.HasValue ?
                    new KValueInfo(
                        response.Value.Data.ToByteArray(),
                        response.Value.Version,
                        DateTime.UtcNow + response.Value.Ttl.ToTimeSpan()) :
                    (KValueInfo?)null);
        }

        /// <summary>
        /// Decodes a list of peers.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        IEnumerable<KNodeInfo<TNodeId>> Decode(IKMessageContext<TNodeId> resources, RepeatedField<Node> nodes)
        {
            foreach (var node in nodes)
                yield return Decode(resources, node);
        }

        /// <summary>
        /// Decodes a single peer.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        KNodeInfo<TNodeId> Decode(IKMessageContext<TNodeId> resources, Node node)
        {
            return new KNodeInfo<TNodeId>(DecodeNodeId(resources, node.Id), node.Endpoints.Select(i => new Uri(i)));
        }

        /// <summary>
        /// Decodes an IPv4 address.
        /// </summary>
        /// <param name="v4"></param>
        /// <returns></returns>
        KIp4Address DecodeIp4Address(uint v4)
        {
            return new KIp4Address(v4);
        }

        /// <summary>
        /// Decodes an IPv6 address.
        /// </summary>
        /// <param name="v6"></param>
        /// <returns></returns>
        KIp6Address DecodeIp6Address(ByteString v6)
        {
#if NET47
            return new KIp6Address(v6.ToByteArray());
#else
            return new KIp6Address(v6.Span);
#endif
        }

    }

}
