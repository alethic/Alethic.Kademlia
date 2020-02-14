using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Provides required operations for node communication within Kademlia.
    /// </summary>
    public interface IKNodeProtocol<TKNodeId, TKNodeData>
        where TKNodeId : struct, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Initiates a PING operation to the remote node and returns its result.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="nodeData"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KNodePingResponse> PingAsync(TKNodeId nodeId, TKNodeData nodeData, CancellationToken cancellationToken);

        /// <summary>
        /// Initiates a STORE operation to the remote node and returns its result.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="nodeData"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KNodeStoreResponse> StoreAsync(TKNodeId nodeId, TKNodeData nodeData, TKNodeId key, CancellationToken cancellationToken);

        /// <summary>
        /// Initiates a FIND_NODE operation to the remote node and returns its result.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="nodeData"></param>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KNodeFindNodeResponse> FindNodeAsync(TKNodeId nodeId, TKNodeData nodeData, TKNodeId key, CancellationToken cancellationToken);

        /// <summary>
        /// Initiates a FIND_VALUE operation to the remote node and returns its result.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="nodeData"></param>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KNodeFindValueResponse> FindValueAsync(TKNodeId nodeId, TKNodeData nodeData, TKNodeId key, CancellationToken cancellationToken);

    }

}
