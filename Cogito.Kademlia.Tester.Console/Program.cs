using System;
using System.Net;
using System.Net.Sockets;
using Cogito.Kademlia.Protocols.Protobuf;

namespace Cogito.Kademlia.Tester.Console
{

    public class Program
    {

        public static void Main(string[] args)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Any, 0));

            var enc = new KProtobufMessageEncoder<KNodeId32>();
            var dec = new KProtobufMessageDecoder<KNodeId32>();


        }

    }

}
