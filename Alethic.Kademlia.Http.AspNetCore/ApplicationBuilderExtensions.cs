using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Alethic.Kademlia.Http.AspNetCore
{

    public static class ApplicationBuilderExtensions
    {

        /// <summary>
        /// Adds the Kademlia protocol to the application builder.
        /// </summary>
        /// <typeparam name="TNodeId"></typeparam>
        /// <param name="app"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseKademlia<TNodeId>(this IApplicationBuilder app, PathString prefix = default)
            where TNodeId : unmanaged
        {
            return app.Map(prefix, b => b.UseMiddleware<KademliaMiddleware<TNodeId>>());
        }

    }

}
