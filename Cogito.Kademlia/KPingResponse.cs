using System;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a response to a PING request.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public readonly struct KPingResponse<TNodeId> : IKResponseBody<TNodeId>, IKRequestBody<TNodeId>
        where TNodeId : unmanaged
    {

        readonly IKProtocolEndpoint<TNodeId>[] endpoints;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="endpoints"></param>
        public KPingResponse(IKProtocolEndpoint<TNodeId>[] endpoints)
        {
            this.endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
        }

        /// <summary>
        /// Gets the set of endpoints to return to the ping requester.
        /// </summary>
        public IKProtocolEndpoint<TNodeId>[] Endpoints => endpoints;

    }

}
