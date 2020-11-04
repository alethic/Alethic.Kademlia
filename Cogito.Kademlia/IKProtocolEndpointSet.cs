using System.Collections.Generic;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Tracks a set of endpoints, managing their position within the set based on their timeout or success events.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public interface IKProtocolEndpointSet<TNodeId> : IEnumerable<IKProtocolEndpoint<TNodeId>>
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Acquires the first available endpoint from the set to use.
        /// </summary>
        /// <returns></returns>
        IKProtocolEndpoint<TNodeId> Acquire();

        /// <summary>
        /// Returns the current information about the endpoint.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        KEndpointInfo<TNodeId> Select(IKProtocolEndpoint<TNodeId> endpoint);

        /// <summary>
        /// Promotes the item to the top of the endpoint list.
        /// </summary>
        /// <param name="endpoint"></param>
        KEndpointInfo<TNodeId> Update(IKProtocolEndpoint<TNodeId> endpoint);

        /// <summary>
        /// Demotes the item to the end of the endpoint list.
        /// </summary>
        /// <param name="endpoint"></param>
        KEndpointInfo<TNodeId> Demote(IKProtocolEndpoint<TNodeId> endpoint);

        /// <summary>
        /// Removes the endpoint and the removed information.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        KEndpointInfo<TNodeId> Remove(IKProtocolEndpoint<TNodeId> endpoint);

    }

}
