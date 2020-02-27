using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

using Cogito.Kademlia.Network;

using Google.Protobuf;
using Google.Protobuf.Collections;

namespace Cogito.Kademlia.Protocols.Protobuf
{

    /// <summary>
    /// Implements a <see cref="IKMessageDecoder{TKNodeId}"/> using Google's Protocol Buffers.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public class KProtobufMessageDecoder<TKNodeId> : IKMessageDecoder<TKNodeId, IKIpProtocolResourceProvider<TKNodeId>>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        public KMessageSequence<TKNodeId> Decode(IKIpProtocolResourceProvider<TKNodeId> resources, ReadOnlySequence<byte> buffer)
        {
            var p = Packet.Parser.ParseFrom(buffer.ToArray());
            var s = new KMessageSequence<TKNodeId>(p.Network, Decode(resources, p.Messages));
            return s;
        }

        IEnumerable<IKMessage<TKNodeId>> Decode(IKIpProtocolResourceProvider<TKNodeId> resources, RepeatedField<Message> messages)
        {
            foreach (var message in messages)
                yield return Decode(resources, message);
        }

        IKMessage<TKNodeId> Decode(IKIpProtocolResourceProvider<TKNodeId> resources, Message message)
        {
            switch (message.BodyCase)
            {
                case Message.BodyOneofCase.PingRequest:
                    return Create(resources, Decode(resources, message.Header), Decode(resources, message.PingRequest));
                case Message.BodyOneofCase.PingResponse:
                    return Create(resources, Decode(resources, message.Header), Decode(resources, message.PingResponse));
                case Message.BodyOneofCase.StoreRequest:
                    return Create(resources, Decode(resources, message.Header), Decode(resources, message.StoreRequest));
                case Message.BodyOneofCase.StoreResponse:
                    return Create(resources, Decode(resources, message.Header), Decode(resources, message.StoreResponse));
                case Message.BodyOneofCase.FindNodeRequest:
                    return Create(resources, Decode(resources, message.Header), Decode(resources, message.FindNodeRequest));
                case Message.BodyOneofCase.FindNodeResponse:
                    return Create(resources, Decode(resources, message.Header), Decode(resources, message.FindNodeResponse));
                case Message.BodyOneofCase.FindValueRequest:
                    return Create(resources, Decode(resources, message.Header), Decode(resources, message.FindValueRequest));
                case Message.BodyOneofCase.FindValueResponse:
                    return Create(resources, Decode(resources, message.Header), Decode(resources, message.FindValueResponse));
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
        TKNodeId DecodeNodeId(IKIpProtocolResourceProvider<TKNodeId> resources, ByteString bytes)
        {
#if NET47
            return KNodeId<TKNodeId>.Read(bytes.ToByteArray());
#else
            return KNodeId<TKNodeId>.Read(bytes.Span);
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
            return new KStoreRequest<TKNodeId>(DecodeNodeId(resources, request.Key), request.Value?.ToByteArray(), request.Ttl != null ? DateTimeOffset.UtcNow + request.Ttl.ToTimeSpan() : (DateTimeOffset?)null);
        }

        /// <summary>
        /// Decodes a STORE response.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        KStoreResponse<TKNodeId> Decode(IKIpProtocolResourceProvider<TKNodeId> resources, StoreResponse response)
        {
            return new KStoreResponse<TKNodeId>(DecodeNodeId(resources, response.Key));
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
            return new KFindNodeResponse<TKNodeId>(DecodeNodeId(resources, response.Key), Decode(resources, response.Peers).ToArray());
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
            return new KFindValueResponse<TKNodeId>(DecodeNodeId(resources, response.Key), Decode(resources, response.Peers).ToArray(), response.Value?.ToByteArray(), response.Ttl != null ? DateTimeOffset.UtcNow + response.Ttl.ToTimeSpan() : (DateTimeOffset?)null);
        }

        /// <summary>
        /// Decodes a list of peers.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="peers"></param>
        /// <returns></returns>
        IEnumerable<KPeerEndpointInfo<TKNodeId>> Decode(IKIpProtocolResourceProvider<TKNodeId> resources, RepeatedField<Peer> peers)
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
        IEnumerable<IKEndpoint<TKNodeId>> Decode(IKIpProtocolResourceProvider<TKNodeId> resources, RepeatedField<IpEndpoint> endpoints)
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
            switch (endpoint.Address.IpAddressCase)
            {
                case IpAddress.IpAddressOneofCase.V4:
                    return resources.CreateEndpoint(new KIpEndpoint(DecodeIp4Address(endpoint.Address.V4), endpoint.Port));
                case IpAddress.IpAddressOneofCase.V6:
                    return resources.CreateEndpoint(new KIpEndpoint(DecodeIp6Address(endpoint.Address.V6), endpoint.Port));
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
