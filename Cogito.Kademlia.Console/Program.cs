using System.Threading.Tasks;

using Autofac;
using Autofac.Extensions.DependencyInjection;

using Cogito.Autofac;
using Cogito.Autofac.DependencyInjection;
using Cogito.Extensions.Options.Autofac;
using Cogito.Extensions.Options.Configuration.Autofac;
using Cogito.Kademlia.Http;
using Cogito.Kademlia.Http.AspNetCore;
using Cogito.Kademlia.InMemory;
using Cogito.Kademlia.Json;
using Cogito.Kademlia.MessagePack;
using Cogito.Kademlia.Network.Udp;
using Cogito.Kademlia.Protobuf;
using Cogito.Serilog;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
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
                return configuration.MinimumLevel.Debug();
            }

        }

        static void RegisterKademlia(ContainerBuilder builder, ulong networkId)
        {
            builder.RegisterType<KJsonMessageFormat<KNodeId256>>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KProtobufMessageFormat<KNodeId256>>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KMessagePackMessageFormat<KNodeId256>>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KRefresher<KNodeId256>>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KConnector<KNodeId256>>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KInvoker<KNodeId256>>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KInvokerPolicy<KNodeId256>>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KRequestHandler<KNodeId256>>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KFixedTableRouter<KNodeId256>>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KLookup<KNodeId256>>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KValueAccessor<KNodeId256>>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KInMemoryStore<KNodeId256>>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KInMemoryPublisher<KNodeId256>>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KHost<KNodeId256>>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KUdpProtocol<KNodeId256>>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KUdpMulticastDiscovery<KNodeId256>>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KStaticDiscovery<KNodeId256>>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KHttpProtocol<KNodeId256>>().AsSelf().SingleInstance();
            builder.Configure<KHostOptions<KNodeId256>>(o => { o.NetworkId = networkId; o.NodeId = KNodeId<KNodeId256>.Create(); });
            builder.Configure<KFixedTableRouterOptions>(o => { });
            builder.Configure<KStaticDiscoveryOptions>(o => { });
            builder.Configure<KHostOptions<KNodeId256>>("Cogito.Kademlia:Host");
            builder.Configure<KFixedTableRouterOptions>("Cogito.Kademlia:FixedTableRouter");
            builder.Configure<KUdpOptions>("Cogito.Kademlia:Udp");
            builder.Configure<KStaticDiscoveryOptions>("Cogito.Kademlia:StaticDiscovery");
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
