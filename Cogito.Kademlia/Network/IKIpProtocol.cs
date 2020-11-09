using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia.Network
{

    /// <summary>
    /// Interface of supporting IP protocols.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public interface IKIpProtocol<TNodeId> : IKProtocol<TNodeId>
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Initiates a request against the endpoint.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KResponse<TNodeId, TResponse>> InvokeAsync<TRequest, TResponse>(KIpProtocolEndpoint<TNodeId> endpoint, in TRequest request, CancellationToken cancellationToken)
            where TRequest : struct, IKRequestBody<TNodeId>
            where TResponse : struct, IKResponseBody<TNodeId>;

    }

}
