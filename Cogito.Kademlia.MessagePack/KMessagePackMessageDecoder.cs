using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

using Cogito.Kademlia.MessagePack.Structures;
using Cogito.Kademlia.Network;

namespace Cogito.Kademlia.MessagePack
{

    /// <summary>
    /// Implements a decoder using MessagePack.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    class KMessagePackMessageDecoder<TNodeId>
        where TNodeId : unmanaged
    {

        public KMessageSequence<TNodeId> Decode(IKMessageContext<TNodeId> context, ReadOnlySequence<byte> buffer)
        {
            var p = global::MessagePack.MessagePackSerializer.Deserialize<Packet>(buffer);
            var s = new KMessageSequence<TNodeId>(p.Network, Decode(context, p.Messages));
            return s;
        }

        IEnumerable<IKMessage<TNodeId>> Decode(IKMessageContext<TNodeId> context, IEnumerable<Message> messages)
        {
            foreach (var message in messages)
                yield return Decode(context, message);
        }

        IKMessage<TNodeId> Decode(IKMessageContext<TNodeId> context, Message message)
        {
            switch (message.Body)
            {
                case PingRequest pi:
                    return Create(context, Decode(context, message.Header), Decode(context, pi));
                case PingResponse pr:
                    return Create(context, Decode(context, message.Header), Decode(context, pr));
                case StoreRequest si:
                    return Create(context, Decode(context, message.Header), Decode(context, si));
                case StoreResponse sr:
                    return Create(context, Decode(context, message.Header), Decode(context, sr));
                case FindNodeRequest fni:
                    return Create(context, Decode(context, message.Header), Decode(context, fni));
                case FindNodeResponse fnr:
                    return Create(context, Decode(context, message.Header), Decode(context, fnr));
                case FindValueRequest fvi:
                    return Create(context, Decode(context, message.Header), Decode(context, fvi));
                case FindValueResponse fvr:
                    return Create(context, Decode(context, message.Header), Decode(context, fvr));
                default:
                    throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Creates a message from the components.
        /// </summary>
        /// <typeparam name="TBody"></typeparam>
        /// <param name="context"></param>
        /// <param name="header"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        IKMessage<TNodeId> Create<TBody>(IKMessageContext<TNodeId> context, KMessageHeader<TNodeId> header, TBody body)
            where TBody : struct, IKRequestBody<TNodeId>
        {
            return new KMessage<TNodeId, TBody>(header, body);
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
        /// <param name="header"></param>
        /// <returns></returns>
        KMessageHeader<TNodeId> Decode(IKMessageContext<TNodeId> context, Header header)
        {
            return new KMessageHeader<TNodeId>(DecodeNodeId(context, header.Sender), header.Magic);
        }

        /// <summary>
        /// Decodes a PING request.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        KPingRequest<TNodeId> Decode(IKMessageContext<TNodeId> context, PingRequest request)
        {
            return new KPingRequest<TNodeId>(Decode(context, request.Endpoints).ToArray());
        }

        /// <summary>
        /// Decodes a PING response.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        KPingResponse<TNodeId> Decode(IKMessageContext<TNodeId> context, PingResponse response)
        {
            return new KPingResponse<TNodeId>(Decode(context, response.Endpoints).ToArray());
        }

        /// <summary>
        /// Decodes a STORE request.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        KStoreRequest<TNodeId> Decode(IKMessageContext<TNodeId> context, StoreRequest request)
        {
            return new KStoreRequest<TNodeId>(
                DecodeNodeId(context, request.Key),
                Decode(context, request.Mode),
                request.HasValue ?
                    new KValueInfo(
                        request.Value.Data,
                        request.Value.Version,
                        DateTime.UtcNow + request.Value.Ttl) :
                    (KValueInfo?)null);
        }

        /// <summary>
        /// Decodes a <see cref="StoreRequest.Types.StoreRequestMode" />.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        KStoreRequestMode Decode(IKMessageContext<TNodeId> context, StoreRequestMode mode)
        {
            return mode switch
            {
                StoreRequestMode.Primary => KStoreRequestMode.Primary,
                StoreRequestMode.Replica => KStoreRequestMode.Replica,
                _ => throw new InvalidOperationException(),
            };
        }

        /// <summary>
        /// Decodes a STORE response.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        KStoreResponse<TNodeId> Decode(IKMessageContext<TNodeId> context, StoreResponse response)
        {
            return new KStoreResponse<TNodeId>(Decode(context, response.Status));
        }

        /// <summary>
        /// Decodes the STORE response status.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        KStoreResponseStatus Decode(IKMessageContext<TNodeId> context, StoreResponseStatus status)
        {
            return status switch
            {
                StoreResponseStatus.Invalid => KStoreResponseStatus.Invalid,
                StoreResponseStatus.Success => KStoreResponseStatus.Success,
                _ => throw new InvalidOperationException(),
            };
        }

        /// <summary>
        /// Decodes a FIND_NODE request.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        KFindNodeRequest<TNodeId> Decode(IKMessageContext<TNodeId> context, FindNodeRequest request)
        {
            return new KFindNodeRequest<TNodeId>(DecodeNodeId(context, request.Key));
        }

        /// <summary>
        /// Decodes a FIND_NODE response.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        KFindNodeResponse<TNodeId> Decode(IKMessageContext<TNodeId> context, FindNodeResponse response)
        {
            return new KFindNodeResponse<TNodeId>(Decode(context, response.Peers).ToArray());
        }

        /// <summary>
        /// Decodes a FIND_NODE request.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        KFindValueRequest<TNodeId> Decode(IKMessageContext<TNodeId> context, FindValueRequest request)
        {
            return new KFindValueRequest<TNodeId>(DecodeNodeId(context, request.Key));
        }

        /// <summary>
        /// Decodes a FIND_NODE response.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        KFindValueResponse<TNodeId> Decode(IKMessageContext<TNodeId> context, FindValueResponse response)
        {
            return new KFindValueResponse<TNodeId>(
                Decode(context, response.Peers).ToArray(),
                response.HasValue ?
                    new KValueInfo(
                        response.Value.Data,
                        response.Value.Version,
                        DateTime.UtcNow + response.Value.Ttl) :
                    (KValueInfo?)null);
        }

        /// <summary>
        /// Decodes a list of peers.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="peers"></param>
        /// <returns></returns>
        IEnumerable<KPeerEndpointInfo<TNodeId>> Decode(IKMessageContext<TNodeId> context, Peer[] peers)
        {
            foreach (var peer in peers)
                yield return Decode(context, peer);
        }

        /// <summary>
        /// Decodes a single peer.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="peer"></param>
        /// <returns></returns>
        KPeerEndpointInfo<TNodeId> Decode(IKMessageContext<TNodeId> context, Peer peer)
        {
            return new KPeerEndpointInfo<TNodeId>(DecodeNodeId(context, peer.Id), new KEndpointSet<TNodeId>(Decode(context, peer.Endpoints)));
        }

        /// <summary>
        /// Decodes a list of endpoints.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="endpoints"></param>
        /// <returns></returns>
        IEnumerable<IKProtocolEndpoint<TNodeId>> Decode(IKMessageContext<TNodeId> context, Uri[] endpoints)
        {
            foreach (var endpoint in endpoints)
                yield return Decode(context, endpoint);
        }

        /// <summary>
        /// Decodes a single endpoint.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        IKProtocolEndpoint<TNodeId> Decode(IKMessageContext<TNodeId> context, Uri endpoint)
        {
            return context.ResolveEndpoint(endpoint);
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
        /// <param name="value"></param>
        /// <returns></returns>
        KIp6Address DecodeIp6Address(byte[] value)
        {
#if NET47
            return new KIp6Address(value);
#else
            return new KIp6Address(value);
#endif
        }

    }

}
