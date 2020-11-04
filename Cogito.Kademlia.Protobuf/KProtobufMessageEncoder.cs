using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

using Google.Protobuf;

namespace Cogito.Kademlia.Protobuf
{

    /// <summary>
    /// Implements a <see cref="IKMessageEncoder{TNodeId}"/> using Google's Protocol Buffers.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    class KProtobufMessageEncoder<TNodeId>
        where TNodeId : unmanaged
    {

        public void Encode(IKMessageContext<TNodeId> context, IBufferWriter<byte> buffer, KMessageSequence<TNodeId> sequence)
        {
            var m = new MemoryStream(1024);

            // generate packet
            var p = new Packet();
            p.Network = sequence.Network;
            p.Messages.AddRange(Encode(context, sequence));
            p.WriteTo(m);

            // reset and dump stream into buffer writer
            m.Position = 0;
            m.ToArray().CopyTo(buffer.GetMemory((int)m.Length));
            buffer.Advance((int)m.Length);
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
                    m.PingRequest = Encode(context, request);
                    break;
                case KPingResponse<TNodeId> request:
                    m.PingResponse = Encode(context, request);
                    break;
                case KStoreRequest<TNodeId> request:
                    m.StoreRequest = Encode(context, request);
                    break;
                case KStoreResponse<TNodeId> request:
                    m.StoreResponse = Encode(context, request);
                    break;
                case KFindNodeRequest<TNodeId> request:
                    m.FindNodeRequest = Encode(context, request);
                    break;
                case KFindNodeResponse<TNodeId> request:
                    m.FindNodeResponse = Encode(context, request);
                    break;
                case KFindValueRequest<TNodeId> request:
                    m.FindValueRequest = Encode(context, request);
                    break;
                case KFindValueResponse<TNodeId> request:
                    m.FindValueResponse = Encode(context, request);
                    break;
                default:
                    throw new InvalidOperationException();
            }

            return m;
        }

        ByteString Encode(IKMessageContext<TNodeId> context, TNodeId nodeId)
        {
#if NET47
            var a = new byte[KNodeId<TNodeId>.SizeOf];
            nodeId.Write(a);
            return ByteString.CopyFrom(a);
#else
            var a = (Span<byte>)stackalloc byte[KNodeId<TNodeId>.SizeOf];
            nodeId.Write(a);
            return ByteString.CopyFrom(a);
#endif
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
            r.Endpoints.Add(Encode(context, request.Endpoints));
            return r;
        }

        PingResponse Encode(IKMessageContext<TNodeId> context, KPingResponse<TNodeId> response)
        {
            var r = new PingResponse();
            r.Endpoints.Add(Encode(context, response.Endpoints));
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
                r.Value.Data = ByteString.CopyFrom(value.Data);
                r.Value.Version = value.Version;
                r.Value.Ttl = new Google.Protobuf.WellKnownTypes.Duration() { Seconds = (long)(value.Expiration - DateTime.UtcNow).TotalSeconds };
            }
            return r;
        }

        StoreRequest.Types.StoreRequestMode Encode(IKMessageContext<TNodeId> context, KStoreRequestMode mode)
        {
            return mode switch
            {
                KStoreRequestMode.Primary => StoreRequest.Types.StoreRequestMode.Primary,
                KStoreRequestMode.Replica => StoreRequest.Types.StoreRequestMode.Replica,
                _ => throw new InvalidOperationException(),
            };
        }

        StoreResponse Encode(IKMessageContext<TNodeId> context, KStoreResponse<TNodeId> response)
        {
            var r = new StoreResponse();
            r.Status = Encode(context, response.Status);
            return r;
        }

        StoreResponse.Types.StoreResponseStatus Encode(IKMessageContext<TNodeId> context, KStoreResponseStatus status)
        {
            return status switch
            {
                KStoreResponseStatus.Invalid => StoreResponse.Types.StoreResponseStatus.Invalid,
                KStoreResponseStatus.Success => StoreResponse.Types.StoreResponseStatus.Success,
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
            r.Peers.Add(Encode(context, response.Peers));
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
                r.Value.Data = ByteString.CopyFrom(value.Data);
                r.Value.Version = value.Version;
                r.Value.Ttl = value.Expiration != null ? new Google.Protobuf.WellKnownTypes.Duration() { Seconds = (long)(value.Expiration - DateTime.UtcNow).TotalSeconds } : null;
            }
            r.Peers.Add(Encode(context, response.Peers));
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
            p.Endpoints.Add(Encode(context, peer.Endpoints));
            return p;
        }

        IEnumerable<IpEndpoint> Encode(IKMessageContext<TNodeId> context, IEnumerable<IKProtocolEndpoint<TNodeId>> endpoints)
        {
            foreach (var endpoint in endpoints)
                if (Encode(context, endpoint) is IpEndpoint ep)
                    yield return ep;
        }

        IpEndpoint Encode(IKMessageContext<TNodeId> context, IKProtocolEndpoint<TNodeId> endpoint)
        {
            return new IpEndpoint() { Uri = endpoint.ToUri().ToString() };
        }

    }

}
