using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

using Cogito.Kademlia.Network;

using Google.Protobuf;

namespace Cogito.Kademlia.Protocols.Protobuf
{

    /// <summary>
    /// Implements a <see cref="IKMessageEncoder{TKNodeId}"/> using Google's Protocol Buffers.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public class KProtobufMessageEncoder<TKNodeId> : IKMessageEncoder<TKNodeId, IKIpProtocolResourceProvider<TKNodeId>>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        public void Encode(IKIpProtocolResourceProvider<TKNodeId> resources, IBufferWriter<byte> buffer, KMessageSequence<TKNodeId> sequence)
        {
            var m = new MemoryStream();

            // generate packet
            var p = new Packet();
            p.Network = sequence.Network;
            p.Messages.AddRange(Encode(resources, sequence));
            p.WriteTo(m);

            // reset and dump stream into buffer writer
            m.Position = 0;
            m.ToArray().CopyTo(buffer.GetMemory((int)m.Length));
            buffer.Advance((int)m.Length);
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
                    m.PingRequest = Encode(resources, request);
                    break;
                case KPingResponse<TKNodeId> request:
                    m.PingResponse = Encode(resources, request);
                    break;
                case KStoreRequest<TKNodeId> request:
                    m.StoreRequest = Encode(resources, request);
                    break;
                case KStoreResponse<TKNodeId> request:
                    m.StoreResponse = Encode(resources, request);
                    break;
                case KFindNodeRequest<TKNodeId> request:
                    m.FindNodeRequest = Encode(resources, request);
                    break;
                case KFindNodeResponse<TKNodeId> request:
                    m.FindNodeResponse = Encode(resources, request);
                    break;
                case KFindValueRequest<TKNodeId> request:
                    m.FindValueRequest = Encode(resources, request);
                    break;
                case KFindValueResponse<TKNodeId> request:
                    m.FindValueResponse = Encode(resources, request);
                    break;
                default:
                    throw new InvalidOperationException();
            }

            return m;
        }

        ByteString Encode(IKIpProtocolResourceProvider<TKNodeId> resources, TKNodeId nodeId)
        {
#if NET47
            var a = new byte[KNodeId<TKNodeId>.SizeOf];
            nodeId.Write(a);
            return ByteString.CopyFrom(a);
#else
            var a = (Span<byte>)stackalloc byte[KNodeId<TKNodeId>.SizeOf];
            nodeId.Write(a);
            return ByteString.CopyFrom(a);
#endif
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
            r.Endpoints.Add(Encode(resources, request.Endpoints));
            return r;
        }

        PingResponse Encode(IKIpProtocolResourceProvider<TKNodeId> resources, KPingResponse<TKNodeId> response)
        {
            var r = new PingResponse();
            r.Endpoints.Add(Encode(resources, response.Endpoints));
            return r;
        }

        StoreRequest Encode(IKIpProtocolResourceProvider<TKNodeId> resources, KStoreRequest<TKNodeId> request)
        {
            var r = new StoreRequest();
            r.Key = Encode(resources, request.Key);
            r.Value = request.Value != null ? ByteString.CopyFrom(request.Value.Value.ToArray()) : null;
            r.Ttl = request.Expiration != null ? new Google.Protobuf.WellKnownTypes.Duration() { Seconds = (long)(request.Expiration.Value - DateTimeOffset.UtcNow).TotalSeconds } : null;
            return r;
        }

        StoreResponse Encode(IKIpProtocolResourceProvider<TKNodeId> resources, KStoreResponse<TKNodeId> response)
        {
            var r = new StoreResponse();
            r.Key = Encode(resources, response.Key);
            return r;
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
            r.Key = Encode(resources, response.Key);
            r.Peers.Add(Encode(resources, response.Peers));
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
            r.Key = Encode(resources, response.Key);
            r.Value = response.Value != null ? ByteString.CopyFrom(response.Value.Value.ToArray()) : null;
            r.Peers.Add(Encode(resources, response.Peers));
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
            p.Endpoints.Add(Encode(resources, peer.Endpoints));
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
                    e.Address = new IpAddress() { V4 = Encode(resources, ip.Endpoint.V4) };
                    break;
                case KIpAddressFamily.IPv6:
                    e.Address = new IpAddress() { V6 = Encode(resources, ip.Endpoint.V6) };
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

        ByteString Encode(IKIpProtocolResourceProvider<TKNodeId> resources, KIp6Address ip)
        {
#if NET47
            var s = new byte[16];
            ip.Write(s);
            return ByteString.CopyFrom(s);
#else
            var s = (Span<byte>)stackalloc byte[16];
            ip.Write(s);
            return ByteString.CopyFrom(s);
#endif
        }

    }

}
