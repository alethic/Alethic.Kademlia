using System.Threading;
using System.Threading.Tasks;

using Autofac;

using Cogito.Autofac;

namespace Cogito.Kademlia.Fabric.Services
{

    public static class Program
    {

        /// <summary>
        /// Main application entry point.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static async Task Main(string[] args)
        {
            var builder = new ContainerBuilder();
            builder.RegisterAllAssemblyModules();

            using (builder.Build())
                await Task.Delay(Timeout.Infinite);
        }

    }

}
