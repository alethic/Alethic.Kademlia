using System.Threading;
using System.Threading.Tasks;

namespace Alethic.Kademlia
{

    /// <summary>
    /// Provides an interface to lookup values within the system.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public interface IKValueAccessor<TNodeId>
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Gets the value of the key from the network.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KValueInfo?> GetAsync(in TNodeId key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets the value of the key into the network.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KValueInfo?> SetAsync(in TNodeId key, in KValueInfo? value, CancellationToken cancellationToken = default);

    }

}
