using System.Collections.Generic;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a list of endpoints.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public interface IKEndpointSet<TKNodeId> : IEnumerable<IKEndpoint<TKNodeId>>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Promotes the item to the top of the endpoint list.
        /// </summary>
        /// <param name="endpoint"></param>
        KEndpointInfo<TKNodeId> Update(IKEndpoint<TKNodeId> endpoint);

        /// <summary>
        /// Demotes the item to the end of the endpoint list.
        /// </summary>
        /// <param name="endpoint"></param>
        KEndpointInfo<TKNodeId> Demote(IKEndpoint<TKNodeId> endpoint);

        /// <summary>
        /// Returns the current information about the endpoint.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        KEndpointInfo<TKNodeId> Select(IKEndpoint<TKNodeId> endpoint);

        /// <summary>
        /// Removes the endpoint and the removed information.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        KEndpointInfo<TKNodeId> Remove(IKEndpoint<TKNodeId> endpoint);

    }

}
