using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a Kademlia routing table.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    /// <typeparam name="TKNodeData"></typeparam>
    public interface IKRoutingTable<TKNodeId, TKNodeData>
        where TKNodeId : struct, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Updates the reference to the node within the table.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="nodeData"></param>
        /// <param name="nodeEvents"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask TouchAsync(TKNodeId nodeId, TKNodeData nodeData = default, IKNodeEvents nodeEvents = null, CancellationToken cancellationToken = default);

    }

}
