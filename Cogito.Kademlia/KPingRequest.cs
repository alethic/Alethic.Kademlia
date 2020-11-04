using System;
using System.Collections.Generic;
using System.Linq;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a PING request.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public readonly struct KPingRequest<TNodeId> : IKRequestBody<TNodeId>, IEquatable<KPingRequest<TNodeId>>
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Creates a response to the given request.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <returns></returns>
        public KPingResponse<TNodeId> Respond(IKProtocolEndpoint<TNodeId>[] endpoints)
        {
            return new KPingResponse<TNodeId>(endpoints);
        }

        /// <summary>
        /// Creates a response to the given request.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <returns></returns>
        public KPingResponse<TNodeId> Respond(IEnumerable<IKProtocolEndpoint<TNodeId>> endpoints)
        {
            return new KPingResponse<TNodeId>(endpoints.ToArray());
        }

        readonly IKProtocolEndpoint<TNodeId>[] endpoints;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="endpoints"></param>
        public KPingRequest(IKProtocolEndpoint<TNodeId>[] endpoints)
        {
            this.endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
        }

        public IKProtocolEndpoint<TNodeId>[] Endpoints => endpoints;

        public bool Equals(KPingRequest<TNodeId> other)
        {
            return other.endpoints.SequenceEqual(endpoints);
        }

        public override bool Equals(object obj)
        {
            return obj is KPingRequest<TNodeId> other && Equals(other);
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
