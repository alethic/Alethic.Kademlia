using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Autofac;

using Cogito.Autofac;
using Cogito.Kademlia.Network;
using Cogito.Kademlia.Protocols.Protobuf;
using Cogito.Kademlia.Protocols.Udp;
using Cogito.Serilog;

using Microsoft.Extensions.Logging;

namespace Cogito.Kademlia.Console
{

    public static class Program
    {

        [RegisterAs(typeof(ILoggerConfigurator))]
        public class LoggerConfigurator : ILoggerConfigurator
        {

            public global::Serilog.LoggerConfiguration Apply(global::Serilog.LoggerConfiguration configuration)
            {
                return configuration.MinimumLevel.Verbose();
            }

        }

        public static async Task Main(string[] args)
        {
            var bld = new ContainerBuilder();
            bld.RegisterAllAssemblyModules();
            var cnt = bld.Build();

            var log = cnt.Resolve<ILogger>();
            var enc = new KProtobufMessageEncoder<KNodeId256>();
            var dec = new KProtobufMessageDecoder<KNodeId256>();
            var slf = KNodeId<KNodeId256>.Create();
            var dat = new KPeerData<KNodeId256>();
            var ink = new KEndpointInvoker<KNodeId256, KPeerData<KNodeId256>>(slf, dat, logger: log);
            var rtr = new KFixedTableRouter<KNodeId256, KPeerData<KNodeId256>>(slf, dat, ink, logger: log);
            var lup = new KLookup<KNodeId256>(rtr, ink, logger: log);
            var str = new KInMemoryStore<KNodeId256>(logger: log);
            var kad = new KEngine<KNodeId256, KPeerData<KNodeId256>>(rtr, ink, lup, str, logger: log);
            var pub = new KInMemoryPublisher<KNodeId256>(ink, lup, str, logger: log);
            var udp = new KUdpProtocol<KNodeId256, KPeerData<KNodeId256>>(2848441, kad, enc, dec, 0, log);
            var mcd = new KUdpMulticastDiscovery<KNodeId256, KPeerData<KNodeId256>>(2848441, kad, udp, enc, dec, new KIpEndpoint(new KIp4Address(IPAddress.Parse("224.168.100.2")), 1283), log);
            await udp.StartAsync();
            await mcd.StartAsync();
            await pub.StartAsync();
            await kad.StartAsync();

            System.Console.WriteLine("Started...");

            var cont = true;
            while (cont)
            {
                switch (System.Console.ReadLine())
                {
                    case "exit":
                        cont = false;
                        break;
                    case "id":
                        System.Console.WriteLine("NodeId:" + kad.SelfId);
                        break;
                    case "peers":
                        foreach (var node in rtr)
                        {
                            System.Console.WriteLine("{0}", node.Key);
                            foreach (var ep in node.Value.Endpoints)
                                System.Console.WriteLine("    {0}", ep);
                        }
                        break;
                    case "lookup":
                        break;
                    case "set":
                        await pub.SetAsync(KNodeId<KNodeId256>.Create(), Encoding.UTF8.GetBytes("test value"), null);
                        break;
                    case "ping":
                        break;
                }
            }

            await kad.StopAsync();
            await mcd.StopAsync();
            await udp.StopAsync();
            System.Console.WriteLine("Stopped...");
            System.Console.ReadLine();
        }

    }

}
