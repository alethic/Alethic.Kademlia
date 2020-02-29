using System;
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
        /// Sets the value in the publisher.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiration"></param>
        /// <param name="version"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KPublisherSetResult<TKNodeId>> SetAsync(in TKNodeId key, ReadOnlyMemory<byte>? value, DateTimeOffset? expiration, ulong? version, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the value in the publisher.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KPublisherGetResult<TKNodeId>> GetAsync(in TKNodeId key, CancellationToken cancellationToken = default);

    }

}
