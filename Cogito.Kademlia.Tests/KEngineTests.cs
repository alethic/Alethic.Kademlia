using System;
using System.Linq;
using System.Threading.Tasks;

using Cogito.Kademlia.InMemory;
using Cogito.Kademlia.Net;
using Cogito.Kademlia.Protocols.Protobuf;
using Cogito.Kademlia.Protocols.Udp;

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
            var dat = new KNodeData<KNodeId32>[s];
            var krt = new KFixedTableRouter<KNodeId32, KNodeData<KNodeId32>>[s];
            var kad = new KEngine<KNodeId32, KNodeData<KNodeId32>>[s];
            var udp = new KUdpProtocol<KNodeId32, KNodeData<KNodeId32>>[s];

            for (int i = 0; i < s; i++)
            {
                kid[i] = KNodeId<KNodeId32>.Create();
                dat[i] = new KNodeData<KNodeId32>();
                var inv = new KEndpointInvoker<KNodeId32, KNodeData<KNodeId32>>(kid[i], dat[i], logger: log.CreateLogger($"Instance {i}"));
                krt[i] = new KFixedTableRouter<KNodeId32, KNodeData<KNodeId32>>(kid[i], dat[i], inv, logger: log.CreateLogger($"Instance {i}"));
                var lku = new KLookup<KNodeId32>(krt[i], inv, logger: log.CreateLogger($"Instance {i}"));
                var str = new KInMemoryStore<KNodeId32>(krt[i], inv, lku, logger: log.CreateLogger($"Instance {i}"));
                kad[i] = new KEngine<KNodeId32, KNodeData<KNodeId32>>(krt[i], inv, lku, str, logger: log.CreateLogger($"Instance {i}"));
                udp[i] = new KUdpProtocol<KNodeId32, KNodeData<KNodeId32>>(13442, kad[i], enc, dec, null, logger: log.CreateLogger($"Instance {i}"));
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
                await Task.Run(() => kad[j].RefreshAsync());
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
