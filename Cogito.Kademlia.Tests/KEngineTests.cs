using System;
using System.Linq;
using System.Threading.Tasks;

using Cogito.Kademlia.Network;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cogito.Kademlia.Tests
{

    [TestClass]
    public class KEngineTests
    {

        [TestMethod]
        public async Task Should_respond_to_ping()
        {
            var s = 5;

            var kid = new KNodeId32[s];
            var krt = new KFixedRoutingTable<KNodeId32, KPeerData<KNodeId32>>[s];
            var kad = new KEngine<KNodeId32, KPeerData<KNodeId32>>[s];
            var udp = new KSimpleUdpNetwork<KNodeId32, KPeerData<KNodeId32>>[s];

            for (int i = 0; i < s; i++)
            {
                kid[i] = KNodeId<KNodeId32>.Create();
                krt[i] = new KFixedRoutingTable<KNodeId32, KPeerData<KNodeId32>>(kid[i], new KPeerData<KNodeId32>());
                kad[i] = new KEngine<KNodeId32, KPeerData<KNodeId32>>(krt[i]);
                udp[i] = new KSimpleUdpNetwork<KNodeId32, KPeerData<KNodeId32>>(kad[i], 0);
                await udp[i].StartAsync();
            }

            await Task.Delay(TimeSpan.FromSeconds(1));

            for (int i = 0; i < s; i++)
            {
                var n = i + 1;
                if (n >= s)
                    n = 0;
                await kad[i].ConnectAsync(udp[i].CreateEndpoint(udp[n].Endpoints.Cast<KIpProtocolEndpoint<KNodeId32>>().First().Endpoint));
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
