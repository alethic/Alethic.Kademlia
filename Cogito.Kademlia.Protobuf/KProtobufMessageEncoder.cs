using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

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
            var p = new MessageSequence();
            p.Network = sequence.Network;
            p.Messages.AddRange(Encode(context, sequence));
            p.WriteTo(buffer);
        }

        IEnumerable<Message> Encode(IKMessageContext<TNodeId> context, IEnumerable<IKMessage<TNodeId>> messages)
        {
            foreach (var message in messages)
                yield return Encode(context, message);
        }

        Message Encode(IKMessageContext<TNodeId> context, IKMessage<TNodeId> message)
        {
            if (message is IKRequest<TNodeId> request)
                return Encode(context, request);
            if (message is IKResponse<TNodeId> response)
                return Encode(context, response);

            throw new InvalidOperationException();
        }

        Message Encode(IKMessageContext<TNodeId> context, IKRequest<TNodeId> request)
        {
            var m = new Message();
            m.Request = new Request();
            m.Request.Header = Encode(context, request.Header);

            switch (request)
            {
                case KRequest<TNodeId, KPingRequest<TNodeId>> ping:
                    m.Request.PingRequest = Encode(context, ping.Body.Value);
                    break;
                case KRequest<TNodeId, KStoreRequest<TNodeId>> store:
                    m.Request.StoreRequest = Encode(context, store.Body.Value);
                    break;
                case KRequest<TNodeId, KFindNodeRequest<TNodeId>> findNode:
                    m.Request.FindNodeRequest = Encode(context, findNode.Body.Value);
                    break;
                case KRequest<TNodeId, KFindValueRequest<TNodeId>> findValue:
                    m.Request.FindValueRequest = Encode(context, findValue.Body.Value);
                    break;
                default:
                    throw new InvalidOperationException();
            }

            return m;
        }

        Message Encode(IKMessageContext<TNodeId> context, IKResponse<TNodeId> message)
        {
            var m = new Message();
            m.Response = new Response();
            m.Response.Header = Encode(context, message.Header);

            switch (message)
            {
                case KResponse<TNodeId, KPingResponse<TNodeId>> ping:
                    m.Response.PingResponse = Encode(context, ping.Body.Value);
                    break;
                case KResponse<TNodeId, KStoreResponse<TNodeId>> store:
                    m.Response.StoreResponse = Encode(context, store.Body.Value);
                    break;
                case KResponse<TNodeId, KFindNodeResponse<TNodeId>> findNode:
                    m.Response.FindNodeResponse = Encode(context, findNode.Body.Value);
                    break;
                case KResponse<TNodeId, KFindValueResponse<TNodeId>> findValue:
                    m.Response.FindValueResponse = Encode(context, findValue.Body.Value);
                    break;
                default:
                    throw new InvalidOperationException();
            }

            return m;
        }

        ByteString Encode(IKMessageContext<TNodeId> context, TNodeId nodeId)
        {
            var a = (Span<byte>)stackalloc byte[KNodeId<TNodeId>.SizeOf];
            nodeId.Write(a);
            return ByteString.CopyFrom(a);
        }

        Header Encode(IKMessageContext<TNodeId> context, KMessageHeader<TNodeId> header)
        {
            var h = new Header();
            h.Sender = Encode(context, header.Sender);
            h.ReplyId = header.ReplyId;
            return h;
        }

        PingRequest Encode(IKMessageContext<TNodeId> context, KPingRequest<TNodeId> request)
        {
            var r = new PingRequest();
            r.Endpoints.Add(request.Endpoints.Select(i => i.ToString()));
            return r;
        }

        PingResponse Encode(IKMessageContext<TNodeId> context, KPingResponse<TNodeId> response)
        {
            var r = new PingResponse();
            r.Endpoints.Add(response.Endpoints.Select(i => i.ToString()));
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
            r.Nodes.Add(Encode(context, response.Nodes));
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
            r.Nodes.Add(Encode(context, response.Nodes));
            return r;
        }

        IEnumerable<Node> Encode(IKMessageContext<TNodeId> context, KNodeInfo<TNodeId>[] nodes)
        {
            foreach (var node in nodes)
                yield return Encode(context, node);
        }

        Node Encode(IKMessageContext<TNodeId> context, KNodeInfo<TNodeId> node)
        {
            var n = new Node();
            n.Id = Encode(context, node.Id);
            n.Endpoints.Add(node.Endpoints.Select(i => i.ToString()));
            return n;
        }

    }

}
