using System;
using System.Collections.Generic;
using System.Linq;

namespace Alethic.Kademlia
{

    /// <summary>
    /// Describes a response to a PING request.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public readonly struct KPingResponse<TNodeId> : IKResponseBody<TNodeId>
        where TNodeId : unmanaged
    {

        readonly Uri[] endpoints;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="endpoints"></param>
        public KPingResponse(Uri[] endpoints)
        {
            this.endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="endpoints"></param>
        public KPingResponse(IEnumerable<Uri> endpoints) :
            this(endpoints.ToArray())
        {

        }

        /// <summary>
        /// Gets the set of endpoints to return to the ping requester.
        /// </summary>
        public Uri[] Endpoints => endpoints;

    }

}
