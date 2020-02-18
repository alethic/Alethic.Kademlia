using System;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a PING request.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KPingRequest<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly ReadOnlyMemory<IKEndpoint<TKNodeId>> endpoints;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="endpoints"></param>
        public KPingRequest(ReadOnlyMemory<IKEndpoint<TKNodeId>> endpoints)
        {
            this.endpoints = endpoints;
        }

        public ReadOnlyMemory<IKEndpoint<TKNodeId>> Endpoints => endpoints;

    }

}
