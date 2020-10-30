using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia.InMemory
{

    /// <summary>
    /// Describes an object that can route messages between in-memory protocol instances.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public interface IKInMemoryProtocolBroker<TKNodeId>
        where TKNodeId : unmanaged
    {

        /// <summary>
        /// Adds the specified protocol to the broker.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="protocol"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask RegisterAsync(in TKNodeId node, IKInMemoryProtocol<TKNodeId> protocol, CancellationToken cancellationToken);

        /// <summary>
        /// Removes the specified protocol from the broker.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="protocol"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask UnregisterAsync(in TKNodeId node, IKInMemoryProtocol<TKNodeId> protocol, CancellationToken cancellationToken);

        /// <summary>
        /// Routes a PING request.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="target"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KResponse<TKNodeId, KPingResponse<TKNodeId>>> PingAsync(in TKNodeId sender, KInMemoryProtocolEndpoint<TKNodeId> target, in KPingRequest<TKNodeId> request, CancellationToken cancellationToken);

        /// <summary>
        /// Routes a STORE request.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="target"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KResponse<TKNodeId, KStoreResponse<TKNodeId>>> StoreAsync(in TKNodeId sender, KInMemoryProtocolEndpoint<TKNodeId> target, in KStoreRequest<TKNodeId> request, CancellationToken cancellationToken);

        /// <summary>
        /// Routes a FIND_NODE request.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="target"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>> FindNodeAsync(in TKNodeId sender, KInMemoryProtocolEndpoint<TKNodeId> target, in KFindNodeRequest<TKNodeId> request, CancellationToken cancellationToken);

        /// <summary>
        /// Routes a FIND_VALUE request.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="target"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KResponse<TKNodeId, KFindValueResponse<TKNodeId>>> FindValueAsync(in TKNodeId sender, KInMemoryProtocolEndpoint<TKNodeId> target, in KFindValueRequest<TKNodeId> request, CancellationToken cancellationToken);


    }

}