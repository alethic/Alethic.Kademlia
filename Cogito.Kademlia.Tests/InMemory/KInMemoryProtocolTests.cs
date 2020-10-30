using System;
using System.Threading.Tasks;

using Autofac;

using Cogito.Autofac;
using Cogito.Kademlia.InMemory;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Cogito.Kademlia.Tests.InMemory
{

    [TestClass]
    public class KInMemoryProtocolTests
    {

        [TestMethod]
        public async Task Foo()
        {
            var bld = new ContainerBuilder();
            bld.RegisterAllAssemblyModules();
            var cnt = bld.Build();

            var brk = new KInMemoryProtocolBroker<KNodeId256>();

            var log = cnt.Resolve<ILogger>();
            var slf = KNodeId<KNodeId256>.Create();
            var dat = new KNodeData<KNodeId256>();
            var ink = new KEndpointInvoker<KNodeId256, KNodeData<KNodeId256>>(slf, dat, logger: log);
            var rtr = new KFixedTableRouter<KNodeId256, KNodeData<KNodeId256>>(slf, dat, ink, logger: log);
            var lup = new KLookup<KNodeId256>(rtr, ink, logger: log);
            var str = new KInMemoryStore<KNodeId256>(rtr, ink, lup, TimeSpan.FromMinutes(1), logger: log);
            var pub = new KInMemoryPublisher<KNodeId256>(ink, lup, str, logger: log);
            var kad = new KEngine<KNodeId256, KNodeData<KNodeId256>>(rtr, ink, lup, str, logger: log);
            var prt = new KInMemoryProtocol<KNodeId256>(kad, brk, log);
            await str.StartAsync();
            await prt.StartAsync();
            await pub.StartAsync();
            await kad.StartAsync();

            await Task.Delay(TimeSpan.FromMinutes(1));
        }

    }

}
