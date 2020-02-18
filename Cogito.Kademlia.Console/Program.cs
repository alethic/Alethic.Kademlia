using System.Threading.Tasks;

using Cogito.Kademlia.Network;

namespace Cogito.Kademlia.Console
{

    public static class Program
    {

        public static async Task Main(string[] args)
        {
            var rtr = new KFixedRoutingTable<KNodeId32, KPeerData<KNodeId32>>(new KNodeId32(0), new KPeerData<KNodeId32>());
            var kad = new KEngine<KNodeId32, KPeerData<KNodeId32>>(rtr);
            var udp = new KSimpleUdpNetwork<KNodeId32, KPeerData<KNodeId32>>(kad, 6000);
            await udp.StartAsync();

            System.Console.ReadLine();
        }

    }

}
