using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Provides interfaces for looking up nodes and values.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public interface IKLookup<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Initiates a node lookup for the specified key, returning the closest discovered nodes.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KLookupResult<TKNodeId>> LookupNodeAsync(in TKNodeId key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Initiates a value lookup for the specified key, returning the closest discovered nodes and the first occurance of the value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KLookupResult<TKNodeId>> LookupValueAsync(in TKNodeId key, CancellationToken cancellationToken = default);

    }

}
