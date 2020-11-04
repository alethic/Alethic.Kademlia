using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Provides required operations for node communication within Kademlia.
    /// </summary>
    public interface IKProtocol<TNodeId>
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Gets the set of endpoints available for communication with this protocol.
        /// </summary>
        IEnumerable<IKProtocolEndpoint<TNodeId>> Endpoints { get; }

        /// <summary>
        /// Attempts to create an endpoint for the protocol given the specified URI. Can return <c>null</c> if the protocol does not support the endpoint.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        IKProtocolEndpoint<TNodeId> ResolveEndpoint(Uri uri);

        /// <summary>
        /// Initiates a PING operation to the remote node and returns its result.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KResponse<TNodeId, TResponse>> InvokeAsync<TRequest, TResponse>(IKProtocolEndpoint<TNodeId> target, in TRequest request, CancellationToken cancellationToken = default)
            where TRequest : struct, IKRequestBody<TNodeId>
            where TResponse : struct, IKResponseBody<TNodeId>;

    }

}
