using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

using Alethic.Kademlia.MessagePack.Structures;

namespace Alethic.Kademlia.MessagePack
{

    /// <summary>
    /// Encodes message sequences using Message Pack.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    class KMessagePackMessageEncoder<TNodeId>
        where TNodeId : unmanaged
    {

        public void Encode(IKMessageContext<TNodeId> context, IBufferWriter<byte> buffer, KMessageSequence<TNodeId> sequence)
        {
            var p = new MessageSequence();
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
            return message switch
            {
                IKRequest<TNodeId> request => Encode(context, request),
                IKResponse<TNodeId> response => Encode(context, response),
                _ => throw new InvalidOperationException(),
            };
        }

        Request Encode(IKMessageContext<TNodeId> context, IKRequest<TNodeId> message)
        {
            var m = new Request();
            m.Header = Encode(context, message.Header);

            switch (message)
            {
                case IKRequest<TNodeId, KPingRequest<TNodeId>> request:
                    m.Body = Encode(context, request.Body.Value);
                    break;
                case IKRequest<TNodeId, KStoreRequest<TNodeId>> request:
                    m.Body = Encode(context, request.Body.Value);
                    break;
                case IKRequest<TNodeId, KFindNodeRequest<TNodeId>> request:
                    m.Body = Encode(context, request.Body.Value);
                    break;
                case IKRequest<TNodeId, KFindValueRequest<TNodeId>> request:
                    m.Body = Encode(context, request.Body.Value);
                    break;
                    throw new InvalidOperationException();
            }

            return m;
        }

        Response Encode(IKMessageContext<TNodeId> context, IKResponse<TNodeId> message)
        {
            var m = new Response();
            m.Header = Encode(context, message.Header);

            switch (message)
            {
                case IKResponse<TNodeId, KPingResponse<TNodeId>> response:
                    m.Body = Encode(context, response.Body.Value);
                    break;
                case IKResponse<TNodeId, KStoreResponse<TNodeId>> response:
                    m.Body = Encode(context, response.Body.Value);
                    break;
                case IKResponse<TNodeId, KFindNodeResponse<TNodeId>> response:
                    m.Body = Encode(context, response.Body.Value);
                    break;
                case IKResponse<TNodeId, KFindValueResponse<TNodeId>> response:
                    m.Body = Encode(context, response.Body.Value);
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
            h.ReplyId = header.ReplyId;
            return h;
        }

        PingRequest Encode(IKMessageContext<TNodeId> context, in KPingRequest<TNodeId> request)
        {
            var r = new PingRequest();
            r.Endpoints = request.Endpoints;
            return r;
        }

        PingResponse Encode(IKMessageContext<TNodeId> context, in KPingResponse<TNodeId> response)
        {
            var r = new PingResponse();
            r.Endpoints = response.Endpoints;
            return r;
        }

        StoreRequest Encode(IKMessageContext<TNodeId> context, in KStoreRequest<TNodeId> request)
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

        StoreResponse Encode(IKMessageContext<TNodeId> context, in KStoreResponse<TNodeId> response)
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

        FindNodeRequest Encode(IKMessageContext<TNodeId> context, in KFindNodeRequest<TNodeId> request)
        {
            var r = new FindNodeRequest();
            r.Key = Encode(context, request.Key);
            return r;
        }

        FindNodeResponse Encode(IKMessageContext<TNodeId> context, in KFindNodeResponse<TNodeId> response)
        {
            var r = new FindNodeResponse();
            r.Peers = Encode(context, response.Nodes).ToArray();
            return r;
        }

        FindValueRequest Encode(IKMessageContext<TNodeId> context, in KFindValueRequest<TNodeId> request)
        {
            var r = new FindValueRequest();
            r.Key = Encode(context, request.Key);
            return r;
        }

        FindValueResponse Encode(IKMessageContext<TNodeId> context, in KFindValueResponse<TNodeId> response)
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
            r.Peers = Encode(context, response.Nodes).ToArray();
            return r;
        }

        IEnumerable<Node> Encode(IKMessageContext<TNodeId> context, KNodeInfo<TNodeId>[] nodes)
        {
            foreach (var node in nodes)
                yield return Encode(context, node);
        }

        Node Encode(IKMessageContext<TNodeId> context, in KNodeInfo<TNodeId> node)
        {
            var p = new Node();
            p.Id = Encode(context, node.Id);
            p.Endpoints = node.Endpoints;
            return p;
        }

    }

}
