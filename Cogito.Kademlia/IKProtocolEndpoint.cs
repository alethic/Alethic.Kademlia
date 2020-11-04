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
        /// Gets the protocol associated with the endpoint.
        /// </summary>
        IKProtocol<TNodeId> Protocol { get; }

        /// <summary>
        /// Gets the set of media types supported by the endpoint.
        /// </summary>
        IEnumerable<string> Accepts { get; }

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
        ValueTask<KResponse<TNodeId, TResponseBody>> InvokeAsync<TRequestBody, TResponseBody>(in TRequestBody request, CancellationToken cancellationToken)
            where TRequestBody : struct, IKRequestBody<TNodeId>
            where TResponseBody : struct, IKResponseBody<TNodeId>;

    }

}
