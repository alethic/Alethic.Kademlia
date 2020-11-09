using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes an endpoint provided by a Kademlia network implementation.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public interface IKProtocolEndpoint<TNodeId>
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Returns a <see cref="Uri"/> representation of the endpoint suitable for transmission.
        /// </summary>
        /// <returns></returns>
        Uri ToUri();

        /// <summary>
        /// Initiates a PING operation to the remote node and returns its result.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KResponse<TNodeId, TResponse>> InvokeAsync<TRequest, TResponse>(in TRequest request, CancellationToken cancellationToken)
            where TRequest : struct, IKRequestBody<TNodeId>
            where TResponse : struct, IKResponseBody<TNodeId>;

    }

}
