using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Autofac;

using Cogito.Autofac;
using Cogito.Kademlia.InMemory;
using Cogito.Threading;

using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Cogito.Kademlia.Tests.InMemory
{

    [TestClass]
    public class KInMemoryProtocolTests
    {

        static void RegisterKademlia(ContainerBuilder builder, KNodeId256 selfId)
        {
            var data = new KNodeData<KNodeId256>();
            var parm = new[] { new TypedParameter(typeof(KNodeId256), selfId), new TypedParameter(typeof(KNodeData<KNodeId256>), data) };

            builder.RegisterType<KEndpointInvoker<KNodeId256, KNodeData<KNodeId256>>>().WithParameters(parm).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KFixedTableRouter<KNodeId256, KNodeData<KNodeId256>>>().WithParameters(parm).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KLookup<KNodeId256>>().WithParameters(parm).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KInMemoryStore<KNodeId256>>().WithParameters(parm).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KInMemoryPublisher<KNodeId256>>().WithParameters(parm).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KEngine<KNodeId256, KNodeData<KNodeId256>>>().WithParameters(parm).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KInMemoryProtocol<KNodeId256>>().WithParameters(parm).AsImplementedInterfaces().SingleInstance();
        }

        [TestMethod]
        public async Task Foo()
        {
            var builder = new ContainerBuilder();
            builder.RegisterAllAssemblyModules();
            using var container = builder.Build();

            var scopes = Enumerable.Range(0, 128).Select(i => container.BeginLifetimeScope(b => RegisterKademlia(b, KNodeId<KNodeId256>.Create()))).ToList();
            await scopes.ForEachAsync(async i => await i.Resolve<IEnumerable<IHostedService>>().ForEachAsync(j => j.StartAsync(CancellationToken.None)));
            await Task.Delay(TimeSpan.FromMinutes(1));

            await scopes.ForEachAsync(async i => await i.Resolve<IEnumerable<IHostedService>>().ForEachAsync(j => j.StopAsync(CancellationToken.None)));
        }

    }

}
