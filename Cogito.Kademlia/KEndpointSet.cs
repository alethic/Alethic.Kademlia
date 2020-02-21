using System.Collections.Generic;

using Cogito.Kademlia.Core;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Defines a standard list of endpoints.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public class KEndpointSet<TKNodeId> : OrderedSet<IKEndpoint<TKNodeId>>, IKEndpointSet<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public KEndpointSet() :
            base(EqualityComparer<IKEndpoint<TKNodeId>>.Default)
        {

        }

    }

}
