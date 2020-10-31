using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;

using Cogito.Kademlia.MessagePack.Structures;
using Cogito.Kademlia.Net;
using Cogito.Kademlia.Protocols;

namespace Cogito.Kademlia.MessagePack
{

    /// <summary>
    /// Implements a <see cref="IKMessageEncoder{TKNodeId}"/> using Google's Protocol Buffers.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public class KMessagePackMessageEncoder<TKNodeId> : IKMessageEncoder<TKNodeId, IKIpProtocolResourceProvider<TKNodeId>>
        where TKNodeId : unmanaged
    {

        public void Encode(IKIpProtocolResourceProvider<TKNodeId> resources, IBufferWriter<byte> buffer, KMessageSequence<TKNodeId> sequence)
        {
            var p = new Packet();
            p.Network = sequence.Network;
            p.Messages = Encode(resources, sequence).ToArray();
            global::MessagePack.MessagePackSerializer.Serialize(buffer, p);
        }

        IEnumerable<Message> Encode(IKIpProtocolResourceProvider<TKNodeId> resources, IEnumerable<IKMessage<TKNodeId>> messages)
        {
            foreach (var message in messages)
                yield return Encode(resources, message);
        }

        Message Encode(IKIpProtocolResourceProvider<TKNodeId> resources, IKMessage<TKNodeId> message)
        {
            var m = new Message();
            m.Header = Encode(resources, message.Header);

            switch (message.Body)
            {
                case KPingRequest<TKNodeId> request:
                    m.Body = Encode(resources, request);
                    break;
                case KPingResponse<TKNodeId> request:
                    m.Body = Encode(resources, request);
                    break;
                case KStoreRequest<TKNodeId> request:
                    m.Body = Encode(resources, request);
                    break;
                case KStoreResponse<TKNodeId> request:
                    m.Body = Encode(resources, request);
                    break;
                case KFindNodeRequest<TKNodeId> request:
                    m.Body = Encode(resources, request);
                    break;
                case KFindNodeResponse<TKNodeId> request:
                    m.Body = Encode(resources, request);
                    break;
                case KFindValueRequest<TKNodeId> request:
                    m.Body = Encode(resources, request);
                    break;
                case KFindValueResponse<TKNodeId> request:
                    m.Body = Encode(resources, request);
                    break;
                default:
                    throw new InvalidOperationException();
            }

            return m;
        }

        byte[] Encode(IKIpProtocolResourceProvider<TKNodeId> resources, TKNodeId nodeId)
        {
            var a = new byte[KNodeId<TKNodeId>.SizeOf];
            nodeId.Write(a);
            return a;
        }

        Header Encode(IKIpProtocolResourceProvider<TKNodeId> resources, KMessageHeader<TKNodeId> header)
        {
            var h = new Header();
            h.Sender = Encode(resources, header.Sender);
            h.Magic = header.Magic;
            return h;
        }

        PingRequest Encode(IKIpProtocolResourceProvider<TKNodeId> resources, KPingRequest<TKNodeId> request)
        {
            var r = new PingRequest();
            r.Endpoints = Encode(resources, request.Endpoints).ToArray();
            return r;
        }

        PingResponse Encode(IKIpProtocolResourceProvider<TKNodeId> resources, KPingResponse<TKNodeId> response)
        {
            var r = new PingResponse();
            r.Endpoints = Encode(resources, response.Endpoints).ToArray();
            return r;
        }

        StoreRequest Encode(IKIpProtocolResourceProvider<TKNodeId> resources, KStoreRequest<TKNodeId> request)
        {
            var r = new StoreRequest();
            r.Key = Encode(resources, request.Key);
            r.Mode = Encode(resources, request.Mode);
            if (request.Value is KValueInfo value)
            {
                r.HasValue = true;
                r.Value = new ValueInfo();
                r.Value.Data = value.Data;
                r.Value.Version = value.Version;
                r.Value.Ttl = value.Expiration - DateTime.UtcNow;
            }
            return r;
        }

        StoreRequestMode Encode(IKIpProtocolResourceProvider<TKNodeId> resources, KStoreRequestMode mode)
        {
            return mode switch
            {
                KStoreRequestMode.Primary => StoreRequestMode.Primary,
                KStoreRequestMode.Replica => StoreRequestMode.Replica,
                _ => throw new InvalidOperationException(),
            };
        }

        StoreResponse Encode(IKIpProtocolResourceProvider<TKNodeId> resources, KStoreResponse<TKNodeId> response)
        {
            var r = new StoreResponse();
            r.Status = Encode(resources, response.Status);
            return r;
        }

        StoreResponseStatus Encode(IKIpProtocolResourceProvider<TKNodeId> resources, KStoreResponseStatus status)
        {
            return status switch
            {
                KStoreResponseStatus.Invalid => StoreResponseStatus.Invalid,
                KStoreResponseStatus.Success => StoreResponseStatus.Success,
                _ => throw new InvalidOperationException(),
            };
        }

        FindNodeRequest Encode(IKIpProtocolResourceProvider<TKNodeId> resources, KFindNodeRequest<TKNodeId> request)
        {
            var r = new FindNodeRequest();
            r.Key = Encode(resources, request.Key);
            return r;
        }

        FindNodeResponse Encode(IKIpProtocolResourceProvider<TKNodeId> resources, KFindNodeResponse<TKNodeId> response)
        {
            var r = new FindNodeResponse();
            r.Peers = Encode(resources, response.Peers).ToArray();
            return r;
        }

        FindValueRequest Encode(IKIpProtocolResourceProvider<TKNodeId> resources, KFindValueRequest<TKNodeId> request)
        {
            var r = new FindValueRequest();
            r.Key = Encode(resources, request.Key);
            return r;
        }

        FindValueResponse Encode(IKIpProtocolResourceProvider<TKNodeId> resources, KFindValueResponse<TKNodeId> response)
        {
            var r = new FindValueResponse();
            if (response.Value is KValueInfo value)
            {
                r.HasValue = true;
                r.Value = new ValueInfo();
                r.Value.Data = value.Data;
                r.Value.Version = value.Version;
                r.Value.Ttl = value.Expiration - DateTime.UtcNow;
            }
            r.Peers = Encode(resources, response.Peers).ToArray();
            return r;
        }

        IEnumerable<Peer> Encode(IKIpProtocolResourceProvider<TKNodeId> resources, KPeerEndpointInfo<TKNodeId>[] peers)
        {
            foreach (var peer in peers)
                yield return Encode(resources, peer);
        }

        Peer Encode(IKIpProtocolResourceProvider<TKNodeId> resources, KPeerEndpointInfo<TKNodeId> peer)
        {
            var p = new Peer();
            p.Id = Encode(resources, peer.Id);
            p.Endpoints = Encode(resources, peer.Endpoints).ToArray();
            return p;
        }

        IEnumerable<IpEndpoint> Encode(IKIpProtocolResourceProvider<TKNodeId> resources, IEnumerable<IKEndpoint<TKNodeId>> endpoints)
        {
            foreach (var endpoint in endpoints)
                if (Encode(resources, endpoint) is IpEndpoint ep)
                    yield return ep;
        }

        IpEndpoint Encode(IKIpProtocolResourceProvider<TKNodeId> resources, IKEndpoint<TKNodeId> endpoint)
        {
            // we only support IP protocol endpoints
            var ip = endpoint as KIpProtocolEndpoint<TKNodeId>;
            if (ip is null)
                return null;

            var e = new IpEndpoint();

            switch (ip.Endpoint.Protocol)
            {
                case KIpAddressFamily.IPv4:
                    e.Address = new Ipv4Address() { Value = Encode(resources, ip.Endpoint.V4) };
                    break;
                case KIpAddressFamily.IPv6:
                    e.Address = new Ipv6Address() { Value = Encode(resources, ip.Endpoint.V6) };
                    break;
                default:
                    throw new InvalidOperationException();
            }

            e.Port = ip.Endpoint.Port;
            return e;
        }

        uint Encode(IKIpProtocolResourceProvider<TKNodeId> resources, KIp4Address ip)
        {
            var s = (Span<byte>)stackalloc byte[4];
            ip.Write(s);
            return BinaryPrimitives.ReadUInt32BigEndian(s);
        }

        byte[] Encode(IKIpProtocolResourceProvider<TKNodeId> resources, KIp6Address ip)
        {
            var s = new byte[16];
            ip.Write(s);
            return s;
        }

    }

}
