using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

using Cogito.Kademlia.MessagePack.Structures;
using Cogito.Kademlia.Net;
using Cogito.Kademlia.Protocols;

namespace Cogito.Kademlia.MessagePack
{

    /// <summary>
    /// Implements a <see cref="IKMessageDecoder{TKNodeId}"/> using MessagePack.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public class KMessagePackMessageDecoder<TKNodeId> : IKMessageDecoder<TKNodeId, IKIpProtocolResourceProvider<TKNodeId>>
        where TKNodeId : unmanaged
    {

        public KMessageSequence<TKNodeId> Decode(IKIpProtocolResourceProvider<TKNodeId> resources, ReadOnlySequence<byte> buffer)
        {
            var p = global::MessagePack.MessagePackSerializer.Deserialize<Packet>(buffer);
            var s = new KMessageSequence<TKNodeId>(p.Network, Decode(resources, p.Messages));
            return s;
        }

        IEnumerable<IKMessage<TKNodeId>> Decode(IKIpProtocolResourceProvider<TKNodeId> resources, IEnumerable<Message> messages)
        {
            foreach (var message in messages)
                yield return Decode(resources, message);
        }

        IKMessage<TKNodeId> Decode(IKIpProtocolResourceProvider<TKNodeId> resources, Message message)
        {
            switch (message.Body)
            {
                case PingRequest pi:
                    return Create(resources, Decode(resources, message.Header), Decode(resources, pi));
                case PingResponse pr:
                    return Create(resources, Decode(resources, message.Header), Decode(resources, pr));
                case StoreRequest si:
                    return Create(resources, Decode(resources, message.Header), Decode(resources, si));
                case StoreResponse sr:
                    return Create(resources, Decode(resources, message.Header), Decode(resources, sr));
                case FindNodeRequest fni:
                    return Create(resources, Decode(resources, message.Header), Decode(resources, fni));
                case FindNodeResponse fnr:
                    return Create(resources, Decode(resources, message.Header), Decode(resources, fnr));
                case FindValueRequest fvi:
                    return Create(resources, Decode(resources, message.Header), Decode(resources, fvi));
                case FindValueResponse fvr:
                    return Create(resources, Decode(resources, message.Header), Decode(resources, fvr));
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
        /// <param name="header"></param>
        /// <returns></returns>
        KMessageHeader<TKNodeId> Decode(IKIpProtocolResourceProvider<TKNodeId> resources, Header header)
        {
            return new KMessageHeader<TKNodeId>(DecodeNodeId(resources, header.Sender), header.Magic);
        }

        /// <summary>
        /// Decodes a PING request.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        KPingRequest<TKNodeId> Decode(IKIpProtocolResourceProvider<TKNodeId> resources, PingRequest request)
        {
            return new KPingRequest<TKNodeId>(Decode(resources, request.Endpoints).ToArray());
        }

        /// <summary>
        /// Decodes a PING response.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        KPingResponse<TKNodeId> Decode(IKIpProtocolResourceProvider<TKNodeId> resources, PingResponse response)
        {
            return new KPingResponse<TKNodeId>(Decode(resources, response.Endpoints).ToArray());
        }

        /// <summary>
        /// Decodes a STORE request.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        KStoreRequest<TKNodeId> Decode(IKIpProtocolResourceProvider<TKNodeId> resources, StoreRequest request)
        {
            return new KStoreRequest<TKNodeId>(
                DecodeNodeId(resources, request.Key),
                Decode(resources, request.Mode),
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
        /// <param name="resources"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        KStoreRequestMode Decode(IKIpProtocolResourceProvider<TKNodeId> resources, StoreRequestMode mode)
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
        /// <param name="resources"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        KStoreResponse<TKNodeId> Decode(IKIpProtocolResourceProvider<TKNodeId> resources, StoreResponse response)
        {
            return new KStoreResponse<TKNodeId>(Decode(resources, response.Status));
        }

        /// <summary>
        /// Decodes the STORE response status.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        KStoreResponseStatus Decode(IKIpProtocolResourceProvider<TKNodeId> resources, StoreResponseStatus status)
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
        /// <param name="resources"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        KFindNodeRequest<TKNodeId> Decode(IKIpProtocolResourceProvider<TKNodeId> resources, FindNodeRequest request)
        {
            return new KFindNodeRequest<TKNodeId>(DecodeNodeId(resources, request.Key));
        }

        /// <summary>
        /// Decodes a FIND_NODE response.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        KFindNodeResponse<TKNodeId> Decode(IKIpProtocolResourceProvider<TKNodeId> resources, FindNodeResponse response)
        {
            return new KFindNodeResponse<TKNodeId>(Decode(resources, response.Peers).ToArray());
        }

        /// <summary>
        /// Decodes a FIND_NODE request.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        KFindValueRequest<TKNodeId> Decode(IKIpProtocolResourceProvider<TKNodeId> resources, FindValueRequest request)
        {
            return new KFindValueRequest<TKNodeId>(DecodeNodeId(resources, request.Key));
        }

        /// <summary>
        /// Decodes a FIND_NODE response.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        KFindValueResponse<TKNodeId> Decode(IKIpProtocolResourceProvider<TKNodeId> resources, FindValueResponse response)
        {
            return new KFindValueResponse<TKNodeId>(
                Decode(resources, response.Peers).ToArray(),
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
        /// <param name="resources"></param>
        /// <param name="peers"></param>
        /// <returns></returns>
        IEnumerable<KPeerEndpointInfo<TKNodeId>> Decode(IKIpProtocolResourceProvider<TKNodeId> resources, Peer[] peers)
        {
            foreach (var peer in peers)
                yield return Decode(resources, peer);
        }

        /// <summary>
        /// Decodes a single peer.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="peer"></param>
        /// <returns></returns>
        KPeerEndpointInfo<TKNodeId> Decode(IKIpProtocolResourceProvider<TKNodeId> resources, Peer peer)
        {
            return new KPeerEndpointInfo<TKNodeId>(DecodeNodeId(resources, peer.Id), new KEndpointSet<TKNodeId>(Decode(resources, peer.Endpoints)));
        }

        /// <summary>
        /// Decodes a list of endpoints.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="endpoints"></param>
        /// <returns></returns>
        IEnumerable<IKEndpoint<TKNodeId>> Decode(IKIpProtocolResourceProvider<TKNodeId> resources, IpEndpoint[] endpoints)
        {
            foreach (var endpoint in endpoints)
                yield return Decode(resources, endpoint);
        }

        /// <summary>
        /// Decodes a single endpoint.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        IKEndpoint<TKNodeId> Decode(IKIpProtocolResourceProvider<TKNodeId> resources, IpEndpoint endpoint)
        {
            switch (endpoint.Address)
            {
                case Ipv4Address ipv4:
                    return resources.CreateEndpoint(new KIpEndpoint(DecodeIp4Address(ipv4.Value), endpoint.Port));
                case Ipv6Address ipv6:
                    return resources.CreateEndpoint(new KIpEndpoint(DecodeIp6Address(ipv6.Value), endpoint.Port));
                default:
                    throw new InvalidOperationException();
            }
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
