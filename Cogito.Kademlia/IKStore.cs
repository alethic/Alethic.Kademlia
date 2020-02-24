using System;
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
        /// <param name="value"></param>
        /// <param name="expiration"></param>
        /// <returns></returns>
        ValueTask<KStoreSetResult<TKNodeId>> SetAsync(in TKNodeId key, ReadOnlyMemory<byte>? value, DateTimeOffset? expiration);

        /// <summary>
        /// Gets the value from the value store.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        ValueTask<KStoreGetResult<TKNodeId>> GetAsync(in TKNodeId key);

    }

}
