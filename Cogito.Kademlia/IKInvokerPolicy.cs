using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a component that can invoke endpoint methods across a set of multiple endpoints.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public interface IKInvokerPolicy<TNodeId>
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Attempts to invoke the request on one of the endpoints.
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="endpoints"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KResponse<TNodeId, TResponse>> InvokeAsync<TRequest, TResponse>(KEndpointSet<TNodeId> endpoints, in TRequest request, CancellationToken cancellationToken = default)
            where TRequest : struct, IKRequestBody<TNodeId>
            where TResponse : struct, IKResponseBody<TNodeId>;

    }

}
