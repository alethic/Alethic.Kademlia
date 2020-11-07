using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Provides an interface to lookup values within the system.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public class KValueAccessor<TNodeId> : IKValueAccessor<TNodeId>
        where TNodeId : unmanaged
    {

        readonly IKStore<TNodeId> store;
        readonly IKLookup<TNodeId> lookup;
        readonly IKInvoker<TNodeId> invoker;
        readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="store"></param>
        /// <param name="invoker"></param>
        /// <param name="logger"></param>
        public KValueAccessor(IKStore<TNodeId> store, IKLookup<TNodeId> nodes, IKInvoker<TNodeId> invoker, ILogger logger)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.lookup = nodes ?? throw new ArgumentNullException(nameof(nodes));
            this.invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the value of the key from the network.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KValueInfo?> GetAsync(in TNodeId key, CancellationToken cancellationToken = default)
        {
            return GetAsync(key, cancellationToken);
        }

        async ValueTask<KValueInfo?> GetAsync(TNodeId key, CancellationToken cancellationToken)
        {
            return await store.GetAsync(key, cancellationToken) ?? (await lookup.LookupValueAsync(key, cancellationToken)).Value;
        }

        /// <summary>
        /// Sets the value of the key into the network.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KValueInfo?> SetAsync(in TNodeId key, in KValueInfo? value, CancellationToken cancellationToken)
        {
            return SetAsync(key, value, cancellationToken);
        }
        async ValueTask<KValueInfo?> SetAsync(TNodeId key, KValueInfo? value, CancellationToken cancellationToken)
        {
            var s = await store.SetAsync(key, KStoreValueMode.Replica, value);
            var r = await lookup.LookupNodeAsync(key, cancellationToken);
            var t = r.Nodes.Select(i => invoker.StoreAsync(i.Endpoints, key, KStoreRequestMode.Primary, value, cancellationToken).AsTask());
            await Task.WhenAll(t);

            return value;
        }

    }

}
