using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
            var kid1 = KNodeId.CreateNodeId<KNodeId32>();
            var kad1 = new KEngine<KNodeId32, KPeerData<KNodeId32>>(new KFixedRoutingTable<KNodeId32, KPeerData<KNodeId32>>(kid1, new KPeerData<KNodeId32>()));
            var udp1 = new KSimpleUdpNetwork<KNodeId32, KPeerData<KNodeId32>>(kad1, 0);
            await udp1.StartAsync();

            var kid2 = KNodeId.CreateNodeId<KNodeId32>();
            var kad2 = new KEngine<KNodeId32, KPeerData<KNodeId32>>(new KFixedRoutingTable<KNodeId32, KPeerData<KNodeId32>>(kid2, new KPeerData<KNodeId32>()));
            var udp2 = new KSimpleUdpNetwork<KNodeId32, KPeerData<KNodeId32>>(kad2, 0);
            await udp2.StartAsync();

            await Task.Delay(TimeSpan.FromSeconds(1));
            await kad1.ConnectAsync(udp1.CreateEndpoint(udp2.Endpoints.Cast<KIpProtocolEndpoint<KNodeId32>>().First().Endpoint));
            await Task.Delay(TimeSpan.FromSeconds(5));

            kad1.Router.Count.Should().Be(1);
            kad2.Router.Count.Should().Be(1);

            await udp1.StopAsync();
            await udp2.StopAsync();
        }

    }

}
