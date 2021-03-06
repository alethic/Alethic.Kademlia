﻿using System.Threading;
using System.Threading.Tasks;

namespace Alethic.Kademlia
{

    /// <summary>
    /// Handles the logic for incoming requests.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public interface IKRequestHandler<TNodeId>
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Invoked to handle incoming PING requests.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KPingResponse<TNodeId>> OnPingAsync(in TNodeId sender, in KPingRequest<TNodeId> request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invoked to handle incoming STORE requests.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KStoreResponse<TNodeId>> OnStoreAsync(in TNodeId sender, in KStoreRequest<TNodeId> request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invoked to handle incoming FIND_NODE requests.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KFindNodeResponse<TNodeId>> OnFindNodeAsync(in TNodeId sender, in KFindNodeRequest<TNodeId> request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invoked to handle incoming FIND_VALUE requests.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KFindValueResponse<TNodeId>> OnFindValueAsync(in TNodeId sender, in KFindValueRequest<TNodeId> request, CancellationToken cancellationToken = default);

    }

}
