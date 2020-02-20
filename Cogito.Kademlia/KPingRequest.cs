using System.Collections.Generic;
using System.Linq;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a PING request.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KPingRequest<TKNodeId> : IKMessageBody<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Creates a response to the given request.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <returns></returns>
        public KPingResponse<TKNodeId> Respond(IKEndpoint<TKNodeId>[] endpoints)
        {
            return new KPingResponse<TKNodeId>(endpoints);
        }

        /// <summary>
        /// Creates a response to the given request.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <returns></returns>
        public KPingResponse<TKNodeId> Respond(IEnumerable<IKEndpoint<TKNodeId>> endpoints)
        {
            return new KPingResponse<TKNodeId>(endpoints.ToArray());
        }

        readonly IKEndpoint<TKNodeId>[] endpoints;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="endpoints"></param>
        public KPingRequest(IKEndpoint<TKNodeId>[] endpoints)
        {
            this.endpoints = endpoints;
        }

        public IKEndpoint<TKNodeId>[] Endpoints => endpoints;

    }

}
