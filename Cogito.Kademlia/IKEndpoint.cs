using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes an endpoint provided by a Kademlia network implementation.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public interface IKEndpoint<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Gets a unique identifier of the protocol available over this endpoint.
        /// </summary>
        Guid ProtocolId { get; }

        /// <summary>
        /// Initiates a PING operation to the remote node and returns its result.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KResponse<TKNodeId, KPingResponse<TKNodeId>>> PingAsync(in KPingRequest<TKNodeId> request, CancellationToken cancellationToken);

        /// <summary>
        /// Initiates a STORE operation to the remote node and returns its result.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KResponse<TKNodeId, KStoreResponse<TKNodeId>>> StoreAsync(in KStoreRequest<TKNodeId> request, CancellationToken cancellationToken);

        /// <summary>
        /// Initiates a FIND_NODE operation to the remote node and returns its result.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>> FindNodeAsync(in KFindNodeRequest<TKNodeId> request, CancellationToken cancellationToken);

        /// <summary>
        /// Initiates a FIND_VALUE operation to the remote node and returns its result.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KResponse<TKNodeId, KFindValueResponse<TKNodeId>>> FindValueAsync(in KFindValueRequest<TKNodeId> request, CancellationToken cancellationToken);

    }

}
