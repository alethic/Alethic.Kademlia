using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a component that implements a value store for values published to the node.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public interface IKStore<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Sets the value in the value store.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="mode"></param>
        /// <param name="value"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KStoreSetResult<TKNodeId>> SetAsync(in TKNodeId key, KStoreValueMode mode, KValueInfo? value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the value from the value store.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KStoreGetResult<TKNodeId>> GetAsync(in TKNodeId key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invoked when a value is changed.
        /// </summary>
        event EventHandler<KValueEventArgs<TKNodeId>> ValueChanged;

    }

}
