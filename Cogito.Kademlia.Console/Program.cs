using System.Threading.Tasks;

using Autofac;
using Autofac.Extensions.DependencyInjection;

using Cogito.Autofac;
using Cogito.Kademlia.InMemory;
using Cogito.Kademlia.Json;
using Cogito.Kademlia.Network.Udp;
using Cogito.Serilog;

using Microsoft.Extensions.Hosting;

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

        static void RegisterKademlia(ContainerBuilder builder, ulong network)
        {
            var self = KNodeId<KNodeId256>.Create();
            var parm = new[] { new TypedParameter(typeof(KNodeId256), self) };

            builder.RegisterType<KJsonMessageFormat<KNodeId256>>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KRefresher<KNodeId256>>().WithParameters(parm).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KInvoker<KNodeId256>>().WithParameters(parm).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KInvokerPolicy<KNodeId256>>().WithParameters(parm).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KConnector<KNodeId256>>().WithParameters(parm).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KRequestHandler<KNodeId256>>().WithParameters(parm).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KFixedTableRouter<KNodeId256>>().WithParameters(parm).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KNodeLookup<KNodeId256>>().WithParameters(parm).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KValueLookup<KNodeId256>>().WithParameters(parm).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KInMemoryStore<KNodeId256>>().WithParameters(parm).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KInMemoryPublisher<KNodeId256>>().WithParameters(parm).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KEngine<KNodeId256>>().WithParameters(parm).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KUdpProtocol<KNodeId256>>().WithParameters(parm).WithParameter("network", network).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KUdpMulticastDiscovery<KNodeId256>>().WithParameters(parm).WithParameter("network", network).AsImplementedInterfaces().SingleInstance();
        }

        /// <summary>
        /// Main application entry point.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static async Task Main(string[] args) =>
            await Host.CreateDefaultBuilder(args)
                .UseServiceProviderFactory(new AutofacServiceProviderFactory(b => { b.RegisterAllAssemblyModules(); RegisterKademlia(b, 40512); }))
                .RunConsoleAsync();

    }

}
