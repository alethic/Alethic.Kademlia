using System;
using System.Threading;
using System.Threading.Tasks;

namespace Alethic.Kademlia
{

    /// <summary>
    /// Describes a component that implements a value store for values published to the node.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public interface IKStore<TNodeId>
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Sets the value in the value store.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="mode"></param>
        /// <param name="value"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<bool> SetAsync(in TNodeId key, KStoreValueMode mode, in KValueInfo? value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the value from the value store.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KValueInfo?> GetAsync(in TNodeId key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invoked when a value is changed.
        /// </summary>
        event EventHandler<KValueEventArgs<TNodeId>> ValueChanged;

    }

}
