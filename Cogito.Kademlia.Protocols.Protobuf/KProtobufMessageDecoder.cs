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
    public class KProtobufMessageDecoder<TKNodeId> : IKMessageDecoder<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        public KMessageSequence<TKNodeId> Decode(IKProtocol<TKNodeId> protocol, ReadOnlySequence<byte> buffer)
        {
            var p = Packet.Parser.ParseFrom(buffer.ToArray());
            var s = new KMessageSequence<TKNodeId>(p.Network, Decode(protocol, p.Messages));
            return s;
        }

        IEnumerable<IKMessage<TKNodeId>> Decode(IKProtocol<TKNodeId> protocol, RepeatedField<Message> messages)
        {
            foreach (var message in messages)
                yield return Decode(protocol, message);
        }

        IKMessage<TKNodeId> Decode(IKProtocol<TKNodeId> protocol, Message message)
        {
            switch (message.BodyCase)
            {
                case Message.BodyOneofCase.PingRequest:
                    return Create(protocol, Decode(protocol, message.Header), Decode(protocol, message.PingRequest));
                case Message.BodyOneofCase.PingResponse:
                    return Create(protocol, Decode(protocol, message.Header), Decode(protocol, message.PingResponse));
                case Message.BodyOneofCase.StoreRequest:
                    return Create(protocol, Decode(protocol, message.Header), Decode(protocol, message.StoreRequest));
                case Message.BodyOneofCase.StoreResponse:
                    return Create(protocol, Decode(protocol, message.Header), Decode(protocol, message.StoreResponse));
                case Message.BodyOneofCase.FindNodeRequest:
                    return Create(protocol, Decode(protocol, message.Header), Decode(protocol, message.FindNodeRequest));
                case Message.BodyOneofCase.FindNodeResponse:
                    return Create(protocol, Decode(protocol, message.Header), Decode(protocol, message.FindNodeResponse));
                case Message.BodyOneofCase.FindValueRequest:
                    return Create(protocol, Decode(protocol, message.Header), Decode(protocol, message.FindValueRequest));
                case Message.BodyOneofCase.FindValueResponse:
                    return Create(protocol, Decode(protocol, message.Header), Decode(protocol, message.FindValueResponse));
                default:
                    throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Creates a message from the components.
        /// </summary>
        /// <typeparam name="TBody"></typeparam>
        /// <param name="protocol"></param>
        /// <param name="header"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        IKMessage<TKNodeId> Create<TBody>(IKProtocol<TKNodeId> protocol, KMessageHeader<TKNodeId> header, TBody body)
            where TBody : struct, IKMessageBody<TKNodeId>
        {
            return new KMessage<TKNodeId, TBody>(header, body);
        }

        /// <summary>
        /// Decodes a <typeparamref name="TKNodeId"/>.
        /// </summary>
        /// <param name="protocol"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        TKNodeId DecodeNodeId(IKProtocol<TKNodeId> protocol, ByteString bytes)
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
        /// <param name="protocol"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        KMessageHeader<TKNodeId> Decode(IKProtocol<TKNodeId> protocol, Header header)
        {
            return new KMessageHeader<TKNodeId>(DecodeNodeId(protocol, header.Sender), header.Magic);
        }

        /// <summary>
        /// Decodes a PING request.
        /// </summary>
        /// <param name="protocol"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        KPingRequest<TKNodeId> Decode(IKProtocol<TKNodeId> protocol, PingRequest request)
        {
            return new KPingRequest<TKNodeId>(Decode(protocol, request.Endpoints).ToArray());
        }

        /// <summary>
        /// Decodes a PING response.
        /// </summary>
        /// <param name="protocol"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        KPingResponse<TKNodeId> Decode(IKProtocol<TKNodeId> protocol, PingResponse response)
        {
            return new KPingResponse<TKNodeId>(Decode(protocol, response.Endpoints).ToArray());
        }

        /// <summary>
        /// Decodes a STORE request.
        /// </summary>
        /// <param name="protocol"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        KStoreRequest<TKNodeId> Decode(IKProtocol<TKNodeId> protocol, StoreRequest request)
        {
            return new KStoreRequest<TKNodeId>(DecodeNodeId(protocol, request.Key), request.Value?.ToByteArray());
        }

        /// <summary>
        /// Decodes a STORE response.
        /// </summary>
        /// <param name="protocol"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        KStoreResponse<TKNodeId> Decode(IKProtocol<TKNodeId> protocol, StoreResponse response)
        {
            return new KStoreResponse<TKNodeId>(DecodeNodeId(protocol, response.Key));
        }

        /// <summary>
        /// Decodes a FIND_NODE request.
        /// </summary>
        /// <param name="protocol"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        KFindNodeRequest<TKNodeId> Decode(IKProtocol<TKNodeId> protocol, FindNodeRequest request)
        {
            return new KFindNodeRequest<TKNodeId>(DecodeNodeId(protocol, request.Key));
        }

        /// <summary>
        /// Decodes a FIND_NODE response.
        /// </summary>
        /// <param name="protocol"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        KFindNodeResponse<TKNodeId> Decode(IKProtocol<TKNodeId> protocol, FindNodeResponse response)
        {
            return new KFindNodeResponse<TKNodeId>(DecodeNodeId(protocol, response.Key), Decode(protocol, response.Peers).ToArray());
        }

        /// <summary>
        /// Decodes a FIND_NODE request.
        /// </summary>
        /// <param name="protocol"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        KFindValueRequest<TKNodeId> Decode(IKProtocol<TKNodeId> protocol, FindValueRequest request)
        {
            return new KFindValueRequest<TKNodeId>(DecodeNodeId(protocol, request.Key));
        }

        /// <summary>
        /// Decodes a FIND_NODE response.
        /// </summary>
        /// <param name="protocol"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        KFindValueResponse<TKNodeId> Decode(IKProtocol<TKNodeId> protocol, FindValueResponse response)
        {
            return new KFindValueResponse<TKNodeId>(DecodeNodeId(protocol, response.Key), response.Value?.ToByteArray(), Decode(protocol, response.Peers).ToArray());
        }

        /// <summary>
        /// Decodes a list of peers.
        /// </summary>
        /// <param name="protocol"></param>
        /// <param name="peers"></param>
        /// <returns></returns>
        IEnumerable<KPeerEndpointInfo<TKNodeId>> Decode(IKProtocol<TKNodeId> protocol, RepeatedField<Peer> peers)
        {
            foreach (var peer in peers)
                yield return Decode(protocol, peer);
        }

        /// <summary>
        /// Decodes a single peer.
        /// </summary>
        /// <param name="protocol"></param>
        /// <param name="peer"></param>
        /// <returns></returns>
        KPeerEndpointInfo<TKNodeId> Decode(IKProtocol<TKNodeId> protocol, Peer peer)
        {
            return new KPeerEndpointInfo<TKNodeId>(DecodeNodeId(protocol, peer.Id), Decode(protocol, peer.Endpoints).ToArray());
        }

        /// <summary>
        /// Decodes a list of endpoints.
        /// </summary>
        /// <param name="protocol"></param>
        /// <param name="endpoints"></param>
        /// <returns></returns>
        IEnumerable<IKEndpoint<TKNodeId>> Decode(IKProtocol<TKNodeId> protocol, RepeatedField<IpEndpoint> endpoints)
        {
            foreach (var endpoint in endpoints)
                yield return Decode(protocol, endpoint);
        }

        /// <summary>
        /// Decodes a single endpoint.
        /// </summary>
        /// <param name="protocol"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        IKEndpoint<TKNodeId> Decode(IKProtocol<TKNodeId> protocol, IpEndpoint endpoint)
        {
            // skip parsing IP addresses for non-IP aware protocol
            var ip = protocol as IKIpProtocol<TKNodeId>;
            if (ip is null)
                return null;

            switch (endpoint.Address.IpAddressCase)
            {
                case IpAddress.IpAddressOneofCase.V4:
                    return ip.CreateEndpoint(new KIpEndpoint(DecodeIp4Address(endpoint.Address.V4), endpoint.Port));
                case IpAddress.IpAddressOneofCase.V6:
                    return ip.CreateEndpoint(new KIpEndpoint(DecodeIp6Address(endpoint.Address.V6), endpoint.Port));
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
