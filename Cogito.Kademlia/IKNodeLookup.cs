using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Provides interfaces for looking up nodes and values.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public interface IKNodeLookup<TNodeId>
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Initiates a node lookup for the specified key, returning the closest discovered nodes.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KNodeLookupNodeResult<TNodeId>> LookupNodeAsync(in TNodeId key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Initiates a value lookup for the specified key, returning the closest discovered nodes and the first occurance of the value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KNodeLookupValueResult<TNodeId>> LookupValueAsync(in TNodeId key, CancellationToken cancellationToken = default);

    }

}
