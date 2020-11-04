using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Provides an interface to lookup values within the system.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public interface IKValueLookup<TNodeId>
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Gets the value of the key from the network.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KValueInfo?> GetValueAsync(in TNodeId key, CancellationToken cancellationToken = default);

    }

}
