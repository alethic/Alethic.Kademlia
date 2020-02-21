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
    public class KProtobufMessageEncoder<TKNodeId> : IKMessageEncoder<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        public void Encode(IKProtocol<TKNodeId> protocol, IBufferWriter<byte> buffer, IEnumerable<IKMessage<TKNodeId>> messages)
        {
            var m = new MemoryStream();

            // generate packet
            var p = new Packet();
            p.Messages.AddRange(Encode(protocol, messages));
            p.WriteTo(m);

            // reset and dump stream into buffer writer
            m.Position = 0;
            m.ToArray().CopyTo(buffer.GetMemory((int)m.Length));
            buffer.Advance((int)m.Length);
        }

        IEnumerable<Message> Encode(IKProtocol<TKNodeId> protocol, IEnumerable<IKMessage<TKNodeId>> messages)
        {
            foreach (var message in messages)
                yield return Encode(protocol, message);
        }

        Message Encode(IKProtocol<TKNodeId> protocol, IKMessage<TKNodeId> message)
        {
            var m = new Message();
            m.Header = Encode(protocol, message.Header);

            switch (message.Body)
            {
                case KPingRequest<TKNodeId> request:
                    m.PingRequest = Encode(protocol, request);
                    break;
                case KPingResponse<TKNodeId> request:
                    m.PingResponse = Encode(protocol, request);
                    break;
                case KStoreRequest<TKNodeId> request:
                    m.StoreRequest = Encode(protocol, request);
                    break;
                case KStoreResponse<TKNodeId> request:
                    m.StoreResponse = Encode(protocol, request);
                    break;
                case KFindNodeRequest<TKNodeId> request:
                    m.FindNodeRequest = Encode(protocol, request);
                    break;
                case KFindNodeResponse<TKNodeId> request:
                    m.FindNodeResponse = Encode(protocol, request);
                    break;
                case KFindValueRequest<TKNodeId> request:
                    m.FindValueRequest = Encode(protocol, request);
                    break;
                case KFindValueResponse<TKNodeId> request:
                    m.FindValueResponse = Encode(protocol, request);
                    break;
                default:
                    throw new InvalidOperationException();
            }

            return m;
        }

        ByteString Encode(IKProtocol<TKNodeId> protocol, TKNodeId nodeId)
        {
#if NET47
            var a = new byte[KNodeId<TKNodeId>.SizeOf()];
            nodeId.Write(a);
            return ByteString.CopyFrom(a);
#else
            var a = (Span<byte>)stackalloc byte[KNodeId<TKNodeId>.SizeOf()];
            nodeId.Write(a);
            return ByteString.CopyFrom(a);
#endif
        }

        Header Encode(IKProtocol<TKNodeId> protocol, KMessageHeader<TKNodeId> header)
        {
            var h = new Header();
            h.Sender = Encode(protocol, header.Sender);
            h.Magic = header.Magic;
            return h;
        }

        PingRequest Encode(IKProtocol<TKNodeId> protocol, KPingRequest<TKNodeId> request)
        {
            var r = new PingRequest();
            r.Endpoints.Add(Encode(protocol, request.Endpoints));
            return r;
        }

        PingResponse Encode(IKProtocol<TKNodeId> protocol, KPingResponse<TKNodeId> response)
        {
            var r = new PingResponse();
            r.Endpoints.Add(Encode(protocol, response.Endpoints));
            return r;
        }

        StoreRequest Encode(IKProtocol<TKNodeId> protocol, KStoreRequest<TKNodeId> request)
        {
            var r = new StoreRequest();
            r.Key = Encode(protocol, request.Key);
            r.Value = request.Value != null ? ByteString.CopyFrom(request.Value.Value.Span) : null;
            return r;
        }

        StoreResponse Encode(IKProtocol<TKNodeId> protocol, KStoreResponse<TKNodeId> response)
        {
            var r = new StoreResponse();
            r.Key = Encode(protocol, response.Key);
            return r;
        }

        FindNodeRequest Encode(IKProtocol<TKNodeId> protocol, KFindNodeRequest<TKNodeId> request)
        {
            var r = new FindNodeRequest();
            r.Key = Encode(protocol, request.Key);
            return r;
        }

        FindNodeResponse Encode(IKProtocol<TKNodeId> protocol, KFindNodeResponse<TKNodeId> response)
        {
            var r = new FindNodeResponse();
            r.Key = Encode(protocol, response.Key);
            r.Peers.Add(Encode(protocol, response.Peers));
            return r;
        }

        FindValueRequest Encode(IKProtocol<TKNodeId> protocol, KFindValueRequest<TKNodeId> request)
        {
            var r = new FindValueRequest();
            r.Key = Encode(protocol, request.Key);
            return r;
        }

        FindValueResponse Encode(IKProtocol<TKNodeId> protocol, KFindValueResponse<TKNodeId> response)
        {
            var r = new FindValueResponse();
            r.Key = Encode(protocol, response.Key);
            r.Value = response.Value != null ? ByteString.CopyFrom(response.Value.Value.Span) : null;
            r.Peers.Add(Encode(protocol, response.Peers));
            return r;
        }

        IEnumerable<Peer> Encode(IKProtocol<TKNodeId> protocol, KPeerEndpointInfo<TKNodeId>[] peers)
        {
            foreach (var peer in peers)
                yield return Encode(protocol, peer);
        }

        Peer Encode(IKProtocol<TKNodeId> protocol, KPeerEndpointInfo<TKNodeId> peer)
        {
            var p = new Peer();
            p.Id = Encode(protocol, peer.Id);
            p.Endpoints.Add(Encode(protocol, peer.Endpoints));
            return p;
        }

        IEnumerable<IpEndpoint> Encode(IKProtocol<TKNodeId> protocol, IEnumerable<IKEndpoint<TKNodeId>> endpoints)
        {
            foreach (var endpoint in endpoints)
                if (Encode(protocol, endpoint) is IpEndpoint ep)
                    yield return ep;
        }

        IpEndpoint Encode(IKProtocol<TKNodeId> protocol, IKEndpoint<TKNodeId> endpoint)
        {
            // we only support IP protocol endpoints
            var ip = endpoint as KIpProtocolEndpoint<TKNodeId>?;
            if (ip is null)
                return null;

            var e = new IpEndpoint();

            switch (ip.Value.Endpoint.Protocol)
            {
                case KIpProtocol.IPv4:
                    e.Address = new IpAddress() { V4 = Encode(protocol, ip.Value.Endpoint.V4) };
                    break;
                case KIpProtocol.IPv6:
                    e.Address = new IpAddress() { V6 = Encode(protocol, ip.Value.Endpoint.V6) };
                    break;
                default:
                    throw new InvalidOperationException();
            }

            e.Port = ip.Value.Endpoint.Port;
            return e;
        }

        uint Encode(IKProtocol<TKNodeId> protocol, KIp4Address ip)
        {
            var s = (Span<byte>)stackalloc byte[4];
            ip.Write(s);
            return BinaryPrimitives.ReadUInt32BigEndian(s);
        }

        ByteString Encode(IKProtocol<TKNodeId> protocol, KIp6Address ip)
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
