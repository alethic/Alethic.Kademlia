using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Provides servics to initiate and refresh connections.
    /// </summary>
    public interface IKConnector<TNodeId>
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Initiates a bootstrap connection to the specified endpoints.
        /// </summary>
        /// <param name="targets"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask ConnectAsync(KProtocolEndpointSet<TNodeId> targets, CancellationToken cancellationToken = default);

        /// <summary>
        /// Initiates a bootstrap connection to the specified endpoint.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask ConnectAsync(IKProtocolEndpoint<TNodeId> endpoint, CancellationToken cancellationToken = default);

        /// <summary>
        /// Initiates a refresh of the Kademlia network by initiating lookups for random node IDs different distances from
        /// the current node ID.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask RefreshAsync(CancellationToken cancellationToken = default);

    }

}
