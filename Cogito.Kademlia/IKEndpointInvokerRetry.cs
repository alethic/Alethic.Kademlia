using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Provides the capability to execute requests against a set of endpoints with failover to the next endpoint.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public interface IKEndpointInvokerRetry<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Executes the function against multiple endpoints until one succeeds.
        /// </summary>
        /// <typeparam name="TResponseBody"></typeparam>
        /// <param name="endpoints"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        ValueTask<KResponse<TKNodeId, TResponseBody>> TryAsync<TResponseBody>(IEnumerable<IKEndpoint<TKNodeId>> endpoints, Func<IKEndpoint<TKNodeId>, ValueTask<KResponse<TKNodeId, TResponseBody>>> func)
            where TResponseBody : struct, IKResponseData<TKNodeId>;

    }

}
