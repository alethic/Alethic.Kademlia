using System.Collections.Generic;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Defines a standard list of endpoints.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public class KEndpointList<TKNodeId> : HashSet<IKEndpoint<TKNodeId>>, IKEndpointList<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="comparer"></param>
        public KEndpointList() :
            base(EqualityComparer<IKEndpoint<TKNodeId>>.Default)
        {

        }

    }

}
