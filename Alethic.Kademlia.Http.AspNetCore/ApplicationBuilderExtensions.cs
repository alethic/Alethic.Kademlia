using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Alethic.Kademlia.Http.AspNetCore
{

    public static class ApplicationBuilderExtensions
    {

        public static IApplicationBuilder UseKademlia<TNodeId>(this IApplicationBuilder app)
            where TNodeId : unmanaged
        {
            return UseKademlia<TNodeId>(app, PathString.Empty);
        }

        public static IApplicationBuilder UseKademlia<TNodeId>(this IApplicationBuilder app, PathString prefix)
            where TNodeId : unmanaged
        {
            return app.Map(prefix, b => b.UseMiddleware<KademliaHttpMiddleware<TNodeId>>());
        }

    }

}
