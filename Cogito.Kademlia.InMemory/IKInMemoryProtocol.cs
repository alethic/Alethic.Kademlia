using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia.InMemory
{

    /// <summary>
    /// Defines a <see cref="IKProtocol{TKNodeId}"/> type that communicates in process.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public interface IKInMemoryProtocol<TKNodeId> : IKProtocol<TKNodeId>, IKInMemoryProtocolResourceProvider<TKNodeId>
        where TKNodeId : unmanaged
    {

        /// <summary>
        /// Invoked by the broker when a node is registered.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        ValueTask OnMemberRegisteredAsync(TKNodeId node);

        /// <summary>
        /// Invoked by the broker when a node is unregistered.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        ValueTask OnMemberUnregisteredAsync(TKNodeId node);

        /// <summary>
        /// Invoked when a PING request is received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="source"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KPingResponse<TKNodeId>> PingRequestAsync(in TKNodeId sender, KInMemoryProtocolEndpoint<TKNodeId> source, in KPingRequest<TKNodeId> request, CancellationToken cancellationToken);

        /// <summary>
        /// Invoked when a STORE request is received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="source"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KStoreResponse<TKNodeId>> StoreRequestAsync(in TKNodeId sender, KInMemoryProtocolEndpoint<TKNodeId> source, in KStoreRequest<TKNodeId> request, CancellationToken cancellationToken);

        /// <summary>
        /// Invoked when a FIND_NODE request is received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="source"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KFindNodeResponse<TKNodeId>> FindNodeRequestAsync(in TKNodeId sender, KInMemoryProtocolEndpoint<TKNodeId> source, in KFindNodeRequest<TKNodeId> request, CancellationToken cancellationToken);

        /// <summary>
        /// Invoked when a FIND_VALUE request is received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="source"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KFindValueResponse<TKNodeId>> FindValueRequestAsync(in TKNodeId sender, KInMemoryProtocolEndpoint<TKNodeId> source, in KFindValueRequest<TKNodeId> request, CancellationToken cancellationToken);

    }

}
