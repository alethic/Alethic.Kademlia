using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;

using Cogito.Kademlia.MessagePack.Structures;
using Cogito.Kademlia.Network;

namespace Cogito.Kademlia.MessagePack
{

    /// <summary>
    /// Implements a <see cref="IKMessageEncoder{TNodeId}"/> using MessagePack.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    class KMessagePackMessageEncoder<TNodeId>
        where TNodeId : unmanaged
    {

        public void Encode(IKMessageContext<TNodeId> context, IBufferWriter<byte> buffer, KMessageSequence<TNodeId> sequence)
        {
            var p = new Packet();
            p.Network = sequence.Network;
            p.Messages = Encode(context, sequence).ToArray();
            global::MessagePack.MessagePackSerializer.Serialize(buffer, p);
        }

        IEnumerable<Message> Encode(IKMessageContext<TNodeId> context, IEnumerable<IKMessage<TNodeId>> messages)
        {
            foreach (var message in messages)
                yield return Encode(context, message);
        }

        Message Encode(IKMessageContext<TNodeId> context, IKMessage<TNodeId> message)
        {
            var m = new Message();
            m.Header = Encode(context, message.Header);

            switch (message.Body)
            {
                case KPingRequest<TNodeId> request:
                    m.Body = Encode(context, request);
                    break;
                case KPingResponse<TNodeId> request:
                    m.Body = Encode(context, request);
                    break;
                case KStoreRequest<TNodeId> request:
                    m.Body = Encode(context, request);
                    break;
                case KStoreResponse<TNodeId> request:
                    m.Body = Encode(context, request);
                    break;
                case KFindNodeRequest<TNodeId> request:
                    m.Body = Encode(context, request);
                    break;
                case KFindNodeResponse<TNodeId> request:
                    m.Body = Encode(context, request);
                    break;
                case KFindValueRequest<TNodeId> request:
                    m.Body = Encode(context, request);
                    break;
                case KFindValueResponse<TNodeId> request:
                    m.Body = Encode(context, request);
                    break;
                default:
                    throw new InvalidOperationException();
            }

            return m;
        }

        byte[] Encode(IKMessageContext<TNodeId> context, TNodeId nodeId)
        {
            var a = new byte[KNodeId<TNodeId>.SizeOf];
            nodeId.Write(a);
            return a;
        }

        Header Encode(IKMessageContext<TNodeId> context, KMessageHeader<TNodeId> header)
        {
            var h = new Header();
            h.Sender = Encode(context, header.Sender);
            h.Magic = header.Magic;
            return h;
        }

        PingRequest Encode(IKMessageContext<TNodeId> context, KPingRequest<TNodeId> request)
        {
            var r = new PingRequest();
            r.Endpoints = Encode(context, request.Endpoints).ToArray();
            return r;
        }

        PingResponse Encode(IKMessageContext<TNodeId> context, KPingResponse<TNodeId> response)
        {
            var r = new PingResponse();
            r.Endpoints = Encode(context, response.Endpoints).ToArray();
            return r;
        }

        StoreRequest Encode(IKMessageContext<TNodeId> context, KStoreRequest<TNodeId> request)
        {
            var r = new StoreRequest();
            r.Key = Encode(context, request.Key);
            r.Mode = Encode(context, request.Mode);
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

        StoreRequestMode Encode(IKMessageContext<TNodeId> context, KStoreRequestMode mode)
        {
            return mode switch
            {
                KStoreRequestMode.Primary => StoreRequestMode.Primary,
                KStoreRequestMode.Replica => StoreRequestMode.Replica,
                _ => throw new InvalidOperationException(),
            };
        }

        StoreResponse Encode(IKMessageContext<TNodeId> context, KStoreResponse<TNodeId> response)
        {
            var r = new StoreResponse();
            r.Status = Encode(context, response.Status);
            return r;
        }

        StoreResponseStatus Encode(IKMessageContext<TNodeId> context, KStoreResponseStatus status)
        {
            return status switch
            {
                KStoreResponseStatus.Invalid => StoreResponseStatus.Invalid,
                KStoreResponseStatus.Success => StoreResponseStatus.Success,
                _ => throw new InvalidOperationException(),
            };
        }

        FindNodeRequest Encode(IKMessageContext<TNodeId> context, KFindNodeRequest<TNodeId> request)
        {
            var r = new FindNodeRequest();
            r.Key = Encode(context, request.Key);
            return r;
        }

        FindNodeResponse Encode(IKMessageContext<TNodeId> context, KFindNodeResponse<TNodeId> response)
        {
            var r = new FindNodeResponse();
            r.Peers = Encode(context, response.Peers).ToArray();
            return r;
        }

        FindValueRequest Encode(IKMessageContext<TNodeId> context, KFindValueRequest<TNodeId> request)
        {
            var r = new FindValueRequest();
            r.Key = Encode(context, request.Key);
            return r;
        }

        FindValueResponse Encode(IKMessageContext<TNodeId> context, KFindValueResponse<TNodeId> response)
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
            r.Peers = Encode(context, response.Peers).ToArray();
            return r;
        }

        IEnumerable<Peer> Encode(IKMessageContext<TNodeId> context, KPeerEndpointInfo<TNodeId>[] peers)
        {
            foreach (var peer in peers)
                yield return Encode(context, peer);
        }

        Peer Encode(IKMessageContext<TNodeId> context, KPeerEndpointInfo<TNodeId> peer)
        {
            var p = new Peer();
            p.Id = Encode(context, peer.Id);
            p.Endpoints = Encode(context, peer.Endpoints).ToArray();
            return p;
        }

        IEnumerable<Uri> Encode(IKMessageContext<TNodeId> context, IEnumerable<IKProtocolEndpoint<TNodeId>> endpoints)
        {
            foreach (var endpoint in endpoints)
                if (Encode(context, endpoint) is Uri uri)
                    yield return uri;
        }

        Uri Encode(IKMessageContext<TNodeId> context, IKProtocolEndpoint<TNodeId> endpoint)
        {
            return endpoint.ToUri();
        }

        uint Encode(IKMessageContext<TNodeId> context, KIp4Address ip)
        {
            var s = (Span<byte>)stackalloc byte[4];
            ip.Write(s);
            return BinaryPrimitives.ReadUInt32BigEndian(s);
        }

        byte[] Encode(IKMessageContext<TNodeId> context, KIp6Address ip)
        {
            var s = new byte[16];
            ip.Write(s);
            return s;
        }

    }

}
