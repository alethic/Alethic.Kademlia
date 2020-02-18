using System.Collections.Generic;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Defines a standard list of endpoints.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public class KEndpointList<TKNodeId> : List<IKEndpoint<TKNodeId>>, IKEndpointList<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {



    }

}
