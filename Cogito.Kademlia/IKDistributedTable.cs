using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Represents the interfaces for acting against the distributed hash table.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public interface IKDistributedTable<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Gets the value for the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<ReadOnlyMemory<byte>?> GetValueAsync(in TKNodeId key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets the given key to the specified value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask SetValueAsync(in TKNodeId key, ReadOnlyMemory<byte>? value, CancellationToken cancellationToken = default);

    }

}
