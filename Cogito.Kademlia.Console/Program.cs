using System.Threading.Tasks;

using Autofac;
using Autofac.Extensions.DependencyInjection;

using Cogito.Autofac;
using Cogito.Kademlia.InMemory;
using Cogito.Kademlia.Json;
using Cogito.Kademlia.Protocols.Udp;
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
            var data = new KNodeData<KNodeId256>();
            var parm = new[] { new TypedParameter(typeof(KNodeId256), self), new TypedParameter(typeof(KNodeData<KNodeId256>), data) };

            builder.RegisterType<KJsonMessageEncoder<KNodeId256>>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KJsonMessageDecoder<KNodeId256>>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KEndpointInvoker<KNodeId256, KNodeData<KNodeId256>>>().WithParameters(parm).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KFixedTableRouter<KNodeId256, KNodeData<KNodeId256>>>().WithParameters(parm).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KLookup<KNodeId256>>().WithParameters(parm).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KInMemoryStore<KNodeId256>>().WithParameters(parm).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KInMemoryPublisher<KNodeId256>>().WithParameters(parm).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KEngine<KNodeId256, KNodeData<KNodeId256>>>().WithParameters(parm).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KUdpProtocol<KNodeId256, KNodeData<KNodeId256>>>().WithParameters(parm).WithParameter("network", network).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KUdpMulticastDiscovery<KNodeId256, KNodeData<KNodeId256>>>().WithParameters(parm).WithParameter("network", network).AsImplementedInterfaces().SingleInstance();
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

        //public static async Task Main(string[] args)
        //{
        //    var bld = new ContainerBuilder();
        //    bld.RegisterAllAssemblyModules();
        //    var cnt = bld.Build();

        //    var ctx = cnt.BeginLifetimeScope(RegisterKademlia);

        //    System.Console.WriteLine("Started...");

        //    var cont = true;
        //    while (cont)
        //    {
        //        var cmd = System.Console.ReadLine().Split(' ');

        //        switch (cmd[0])
        //        {
        //            case "exit":
        //                cont = false;
        //                break;
        //            case "id":
        //                System.Console.WriteLine("NodeId:" + kad.SelfId);
        //                break;
        //            case "peers":
        //                foreach (var node in rtr)
        //                {
        //                    System.Console.WriteLine("{0}", node.Key);
        //                    foreach (var ep in node.Value.Endpoints)
        //                        System.Console.WriteLine("    {0}", ep);
        //                }
        //                break;
        //            case "pub":
        //                {
        //                    var key = KNodeId<KNodeId256>.Read(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(cmd[1])));
        //                    var val = Encoding.UTF8.GetBytes(string.Join(" ", cmd.Skip(2)));
        //                    var ext = await pub.GetAsync(key);
        //                    var ver = ext?.Version ?? 0;
        //                    var inf = new KValueInfo(val, ver + 1, DateTime.UtcNow.AddMinutes(15));
        //                    await pub.AddAsync(key, inf);
        //                }
        //                break;
        //            case "get":
        //                {
        //                    var key = KNodeId<KNodeId256>.Read(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(cmd[1])));
        //                    var val = await kad.GetValueAsync(key);
        //                    if (val != null)
        //                        System.Console.WriteLine("Value: {0}", Encoding.UTF8.GetString(val.Value.Data.ToArray()));
        //                    else
        //                        System.Console.WriteLine("Value missing.");
        //                }
        //                break;
        //            case "ping":
        //                break;
        //        }
        //    }

        //    System.Console.WriteLine("Stopped...");
        //    System.Console.ReadLine();
        //}

    }

}
