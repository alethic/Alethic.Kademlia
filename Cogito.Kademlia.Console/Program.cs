using System.Net;
using System.Threading.Tasks;

using Cogito.Kademlia.Network;

namespace Cogito.Kademlia.Console
{

    public static class Program
    {

        public static async Task Main(string[] args)
        {
            var udp = new KSimpleUdpNetwork<KNodeId32, KIpPeer<KNodeId32>>(IPAddress.IPv6Any, 6000);
            await udp.StartAsync();

            System.Console.ReadLine();
        }

    }

}
