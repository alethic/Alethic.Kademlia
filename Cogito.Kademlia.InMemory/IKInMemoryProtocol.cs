using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia.InMemory
{

    /// <summary>
    /// Defines a <see cref="IKProtocol{TNodeId}"/> type that communicates in process.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public interface IKInMemoryProtocol<TNodeId> : IKProtocol<TNodeId>
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Invoked by the broker when a node is registered.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        ValueTask OnMemberRegisteredAsync(TNodeId node);

        /// <summary>
        /// Invoked by the broker when a node is unregistered.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        ValueTask OnMemberUnregisteredAsync(TNodeId node);

        /// <summary>
        /// Invoked when a PING request is received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="source"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<TResponse> InvokeRequestAsync<TRequest, TResponse>(in TNodeId sender, KInMemoryProtocolEndpoint<TNodeId> source, in TRequest request, CancellationToken cancellationToken)
            where TRequest : struct, IKRequestBody<TNodeId>
            where TResponse : struct, IKResponseBody<TNodeId>;

    }

}
