using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Represents a Kademlia network engine.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    /// <typeparam name="TKPeerData"></typeparam>
    public interface IKEngine<TKNodeId, TKPeerData> : IKEngine<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Gets the peer data of the node itself.
        /// </summary>
        TKPeerData SelfData { get; }

        /// <summary>
        /// Gets the router configured on the engine.
        /// </summary>
        IKRouter<TKNodeId, TKPeerData> Router { get; }

    }

    /// <summary>
    /// Represents a Kademlia network engine.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public interface IKEngine<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Gets the Node ID of the node itself.
        /// </summary>
        TKNodeId SelfId { get; }

        /// <summary>
        /// Gets the value for the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<ReadOnlyMemory<byte>> GetValueAsync(in TKNodeId key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets the given key to the specified value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask SetValueAsync(in TKNodeId key, ReadOnlySpan<byte> value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invoked to handle incoming PING requests.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KPingResponse<TKNodeId>> OnPingAsync(in TKNodeId sender, in KPingRequest<TKNodeId> request, CancellationToken cancellationToken);

        /// <summary>
        /// Invoked to handle incoming STORE requests.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KStoreResponse<TKNodeId>> OnStoreAsync(in TKNodeId sender, in KStoreRequest<TKNodeId> request, CancellationToken cancellationToken);

        /// <summary>
        /// Invoked to handle incoming FIND_NODE requests.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KFindNodeResponse<TKNodeId>> OnFindNodeAsync(in TKNodeId sender, in KFindNodeRequest<TKNodeId> request, CancellationToken cancellationToken);

        /// <summary>
        /// Invoked to handle incoming FIND_VALUE requests.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KFindValueResponse<TKNodeId>> OnFindValueAsync(in TKNodeId sender, in KFindValueRequest<TKNodeId> request, CancellationToken cancellationToken);

    }

}
