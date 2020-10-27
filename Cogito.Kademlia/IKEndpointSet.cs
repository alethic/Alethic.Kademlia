using System.Collections.Generic;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Tracks a set of endpoints, managing their position within the set based on their timeout or success events.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public interface IKEndpointSet<TKNodeId> : IEnumerable<IKEndpoint<TKNodeId>>
        where TKNodeId : unmanaged
    {

        /// <summary>
        /// Acquires the first available endpoint from the set to use.
        /// </summary>
        /// <returns></returns>
        IKEndpoint<TKNodeId> Acquire();

        /// <summary>
        /// Returns the current information about the endpoint.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        KEndpointInfo<TKNodeId> Select(IKEndpoint<TKNodeId> endpoint);

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
        /// Removes the endpoint and the removed information.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        KEndpointInfo<TKNodeId> Remove(IKEndpoint<TKNodeId> endpoint);

    }

}
