using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a Kademlia routing table.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    /// <typeparam name="TKPeerData"></typeparam>
    public interface IKRoutingTable<TKNodeId, TKPeerData>
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
        ValueTask TouchAsync(TKNodeId nodeId, TKPeerData nodeData = default, IKPeerEvents nodeEvents = null, CancellationToken cancellationToken = default);

    }

}
