using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;

using Cogito.Kademlia.InMemory;
using Cogito.Kademlia.Network;
using Cogito.Kademlia.Network.Udp;
using Cogito.Kademlia.Protobuf;
using Cogito.ServiceFabric.Services.Autofac;

using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Services.Runtime;

namespace Cogito.Kademlia.Fabric.Services
{

    [RegisterStatelessService("Cogito.Kademlia.Fabric.Services.KademliaService")]
    public class KademliaService : StatelessService
    {

        readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="logger"></param>
        public KademliaService(StatelessServiceContext context, ILogger logger) :
            base(context)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var fmt = new KProtobufMessageFormat<KNodeId256>();
            var slf = KNodeId<KNodeId256>.Create();
            var ink = new KInvokerPolicy<KNodeId256>(slf, logger: logger);
            var rtr = new KFixedTableRouter<KNodeId256>(slf, logger: logger);
            var kad = new KEngine<KNodeId256>(rtr, ink, lup, str, logger: logger);
            var lup = new KNodeLookup<KNodeId256>(kad, logger: logger);
            var str = new KInMemoryStore<KNodeId256>(rtr, ink, lup, TimeSpan.FromMinutes(1), logger: logger);
            var pub = new KInMemoryPublisher<KNodeId256>(ink, lup, str, logger: logger);
            var udp = new KUdpProtocol<KNodeId256>(2848441, kad, fmt, KIpEndpoint.Any, logger);
            var mcd = new KUdpMulticastDiscovery<KNodeId256>(2848441, kad, fmt, logger: logger);

            await udp.StartAsync(cancellationToken);
            await str.StartAsync(cancellationToken);
            await pub.StartAsync(cancellationToken);
            await mcd.StartAsync(cancellationToken);
            await kad.StartAsync(cancellationToken);

            while (cancellationToken.IsCancellationRequested == false)
                await Task.Delay(TimeSpan.FromSeconds(1));

            await kad.StopAsync();
            await mcd.StopAsync();
            await pub.StopAsync();
            await str.StopAsync();
            await udp.StopAsync();
        }

    }

}
