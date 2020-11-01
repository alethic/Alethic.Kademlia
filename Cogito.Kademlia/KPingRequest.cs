using System;
using System.Collections.Generic;
using System.Linq;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a PING request.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KPingRequest<TKNodeId> : IKMessageBody<TKNodeId>, IEquatable<KPingRequest<TKNodeId>>
        where TKNodeId : unmanaged
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
            this.endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
        }

        public IKEndpoint<TKNodeId>[] Endpoints => endpoints;

        public bool Equals(KPingRequest<TKNodeId> other)
        {
            return other.endpoints.SequenceEqual(endpoints);
        }

        public override bool Equals(object obj)
        {
            return obj is KPingRequest<TKNodeId> other && Equals(other);
        }

        public override int GetHashCode()
        {
            var h = new HashCode();
            h.Add(endpoints.Length);
            foreach (var i in endpoints)
                h.Add(i);
            return h.ToHashCode();
        }

    }

}
