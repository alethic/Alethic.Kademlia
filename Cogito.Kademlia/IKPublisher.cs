using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a component that periodically publishes values owned by the node.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public interface IKPublisher<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Adds a value for the key to the publisher.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<bool> AddAsync(in TKNodeId key, in KValueInfo value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes the value for the specified key from the publisher.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<bool> RemoveAsync(in TKNodeId key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the value for the specified key from the publisher.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KValueInfo?> GetAsync(in TKNodeId key, CancellationToken cancellationToken = default);

    }

}
