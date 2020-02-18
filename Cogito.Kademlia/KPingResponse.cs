using System;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a response to a PING request.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KPingResponse<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly ReadOnlyMemory<IKEndpoint<TKNodeId>> endpoints;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="endpoints"></param>
        public KPingResponse(ReadOnlyMemory<IKEndpoint<TKNodeId>> endpoints)
        {
            this.endpoints = endpoints;
        }

        /// <summary>
        /// Gets the set of endpoints to return to the ping requester.
        /// </summary>
        public ReadOnlyMemory<IKEndpoint<TKNodeId>> Endpoints => endpoints;

    }

}
