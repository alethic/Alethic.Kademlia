using System;
using System.Linq;
using System.Security.Cryptography;
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
            var str = new KInMemoryStore<KNodeId256>(rtr, ink, lup, TimeSpan.FromMinutes(1), logger: log);
            var pub = new KInMemoryPublisher<KNodeId256>(ink, lup, str, logger: log);
            var kad = new KEngine<KNodeId256, KPeerData<KNodeId256>>(rtr, ink, lup, str, logger: log);
            var udp = new KUdpProtocol<KNodeId256, KPeerData<KNodeId256>>(2848441, kad, enc, dec, KIpEndpoint.Any, log);
            var mcd = new KUdpMulticastDiscovery<KNodeId256, KPeerData<KNodeId256>>(2848441, kad, udp, enc, dec, new KIpEndpoint(KIp4Address.Parse("239.255.83.54"), 1283), log);
            await str.StartAsync();
            await udp.StartAsync();
            await mcd.StartAsync();
            await pub.StartAsync();
            await kad.StartAsync();

            System.Console.WriteLine("Started...");

            var cont = true;
            while (cont)
            {
                var cmd = System.Console.ReadLine().Split(' ');

                switch (cmd[0])
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
                    case "pub":
                        {
                            var key = KNodeId<KNodeId256>.Read(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(cmd[1])));
                            var val = Encoding.UTF8.GetBytes(string.Join(" ", cmd.Skip(2)));
                            var ext = await pub.GetAsync(key);
                            var ver = ext?.Version ?? 0;
                            var inf = new KValueInfo(val, ver + 1, DateTime.UtcNow.AddMinutes(15));
                            await pub.AddAsync(key, inf);
                        }
                        break;
                    case "get":
                        {
                            var key = KNodeId<KNodeId256>.Read(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(cmd[1])));
                            var val = (await str.GetAsync(key)).Value ?? (await lup.LookupValueAsync(key)).Value;
                            if (val != null)
                                System.Console.WriteLine("Value: {0}", Encoding.UTF8.GetString(val.Value.Data.ToArray()));
                            else
                                System.Console.WriteLine("Value missing.");
                        }
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
