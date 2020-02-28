using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Cogito.Kademlia.Network;
using Cogito.Kademlia.Protocols;
using Cogito.Kademlia.Protocols.Protobuf;

namespace Cogito.Kademlia.Tester.Console
{

    public class Program
    {

        public static void Main(string[] args)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Any, 0));

            var enc = new KProtobufMessageEncoder<KNodeId256>();
            var dec = new KProtobufMessageDecoder<KNodeId256>();

            var nd = KNodeId<KNodeId256>.Create();
            var nt = 2848441ul;
            var rn = new Random();

            while (true)
            {
                var wrt = new ArrayBufferWriter<byte>();
                enc.Encode(
                    new Prov(),
                    wrt,
                    new Protocols.KMessageSequence<KNodeId256>(nt, new IKMessage<KNodeId256>[] {
                    new KMessage<KNodeId256, KPingRequest<KNodeId256>>(
                        new KMessageHeader<KNodeId256>(nd, (ulong)rn.NextInt64()),
                        new KPingRequest<KNodeId256>(new IKEndpoint<KNodeId256>[0]))
                    }));

                socket.SendTo(wrt.WrittenMemory.ToArray(), new IPEndPoint(IPAddress.Parse("192.168.175.81"), 54779));
                Thread.Sleep(00);

                var exit = false;
                while (exit == false)
                {
                    var buffer = new byte[8192];
                    var length = socket.Receive(buffer);
                    if (length > 0)
                    {
                        System.Console.WriteLine("Received packet.");
                        var seq = dec.Decode(new Prov(), new System.Buffers.ReadOnlySequence<byte>(new ReadOnlyMemory<byte>(buffer).Slice(0, length)));
                        foreach (var msg in seq)
                        {
                            var body = (KPingResponse<KNodeId256>)msg.Body;
                            System.Console.WriteLine("Received {0} endpoints.", body.Endpoints.Length);
                        }
                        exit = true;
                    }
                }
            }
        }

        class Prov : IKProtocol<KNodeId256>, IKIpProtocolResourceProvider<KNodeId256>
        {
            public IEnumerable<IKEndpoint<KNodeId256>> Endpoints => throw new NotImplementedException();

            public ValueTask<KResponse<KNodeId256, KFindNodeResponse<KNodeId256>>> FindNodeAsync(IKEndpoint<KNodeId256> endpoint, in KFindNodeRequest<KNodeId256> request, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public ValueTask<KResponse<KNodeId256, KFindValueResponse<KNodeId256>>> FindValueAsync(IKEndpoint<KNodeId256> endpoint, in KFindValueRequest<KNodeId256> request, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public ValueTask<KResponse<KNodeId256, KPingResponse<KNodeId256>>> PingAsync(IKEndpoint<KNodeId256> endpoint, in KPingRequest<KNodeId256> request, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public ValueTask<KResponse<KNodeId256, KStoreResponse<KNodeId256>>> StoreAsync(IKEndpoint<KNodeId256> endpoint, in KStoreRequest<KNodeId256> request, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            IKEndpoint<KNodeId256> IKIpProtocolResourceProvider<KNodeId256>.CreateEndpoint(in KIpEndpoint endpoint)
            {
                return new KIpProtocolEndpoint<KNodeId256>(this, endpoint);
            }
        }

    }

}
