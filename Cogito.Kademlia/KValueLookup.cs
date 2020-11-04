using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Provides an interface to lookup values within the system.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public class KValueLookup<TNodeId> : IKValueLookup<TNodeId>
        where TNodeId: unmanaged
    {

        readonly IKNodeLookup<TNodeId> nodes;
        readonly IKStore<TNodeId> store;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="store"></param>
        public KValueLookup(IKNodeLookup<TNodeId> nodes, IKStore<TNodeId> store)
        {
            this.nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        /// <summary>
        /// Gets the value of the key from the network.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KValueInfo?> GetValueAsync(in TNodeId key, CancellationToken cancellationToken = default)
        {
            return GetValueAsync(key, cancellationToken);
        }

        async ValueTask<KValueInfo?> GetValueAsync(TNodeId key, CancellationToken cancellationToken)
        {
            return await store.GetAsync(key, cancellationToken) ?? (await nodes.LookupValueAsync(key, cancellationToken)).Value;
        }

    }

}
