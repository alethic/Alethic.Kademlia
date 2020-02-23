using System;
using System.Linq;
using System.Threading.Tasks;

using Cogito.Kademlia.Network;
using Cogito.Kademlia.Protocols;
using Cogito.Kademlia.Protocols.Protobuf;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cogito.Kademlia.Tests
{

    [TestClass]
    public class KEngineTests
    {

        [TestMethod]
        public async Task Should_respond_to_ping()
        {
            var s = 4;

            var log = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));
            var enc = new KProtobufMessageEncoder<KNodeId32>();
            var dec = new KProtobufMessageDecoder<KNodeId32>();
            var kid = new KNodeId32[s];
            var krt = new KFixedRoutingTable<KNodeId32, KPeerData<KNodeId32>>[s];
            var kad = new KEngine<KNodeId32, KPeerData<KNodeId32>>[s];
            var udp = new KUdpProtocol<KNodeId32, KPeerData<KNodeId32>>[s];

            for (int i = 0; i < s; i++)
            {
                kid[i] = KNodeId<KNodeId32>.Create();
                krt[i] = new KFixedRoutingTable<KNodeId32, KPeerData<KNodeId32>>(kid[i], new KPeerData<KNodeId32>(), logger: log.CreateLogger($"Instance {i}"));
                kad[i] = new KEngine<KNodeId32, KPeerData<KNodeId32>>(krt[i], logger: log.CreateLogger($"Instance {i}"));
                udp[i] = new KUdpProtocol<KNodeId32, KPeerData<KNodeId32>>(13442, kad[i], enc, dec, 0, logger: log.CreateLogger($"Instance {i}"));
                await udp[i].StartAsync();
            }

            await Task.Delay(TimeSpan.FromSeconds(1));

            for (int i = 0; i < s; i++)
            {
                var j = i;
                var n = j + 1;
                if (n >= s)
                    n = 0;

                await Task.Run(() => kad[j].ConnectAsync(udp[j].CreateEndpoint(udp[n].Endpoints.Cast<KIpProtocolEndpoint<KNodeId32>>().First().Endpoint)));
                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }

            // initiate a refresh which should populate the tables
            for (int i = 0; i < s; i++)
            {
                var j = i;
                await Task.Run(() => krt[j].RefreshAsync());
            }

            await Task.Delay(TimeSpan.FromSeconds(5));

            for (int i = 0; i < s; i++)
            {
                Console.WriteLine($"Instance {i} ({kid[i]}) has {kad[i].Router.Count} items in it's node table.");
                foreach (var j in krt[i])
                    Console.WriteLine($"    {j.Key}");
                await udp[i].StopAsync();
            }
        }

    }

}
