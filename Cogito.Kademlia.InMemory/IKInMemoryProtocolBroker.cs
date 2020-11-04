using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia.InMemory
{

    /// <summary>
    /// Describes an object that can route messages between in-memory protocol instances.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public interface IKInMemoryProtocolBroker<TNodeId>
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Adds the specified protocol to the broker.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="protocol"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask RegisterAsync(in TNodeId node, IKInMemoryProtocol<TNodeId> protocol, CancellationToken cancellationToken);

        /// <summary>
        /// Removes the specified protocol from the broker.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="protocol"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask UnregisterAsync(in TNodeId node, IKInMemoryProtocol<TNodeId> protocol, CancellationToken cancellationToken);

        /// <summary>
        /// Routes a PING request.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="target"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KResponse<TNodeId, TResponse>> InvokeAsync<TRequest, TResponse>(in TNodeId sender, KInMemoryProtocolEndpoint<TNodeId> target, in TRequest request, CancellationToken cancellationToken)
            where TRequest : struct, IKRequestBody<TNodeId>
            where TResponse : struct, IKResponseBody<TNodeId>;

    }

}