using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Cogito.Threading;

using Microsoft.Extensions.Logging;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Provides a <see cref="IKPublisher{TKNodeId}"/> implementation that keeps published items in memory.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public class KInMemoryPublisher<TKNodeId> : IKPublisher<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Represents an entry in the publisher.
        /// </summary>
        readonly struct Entry
        {

            public readonly KValueInfo Value;

            /// <summary>
            /// Initializes a new instance.
            /// </summary>
            /// <param name="value"></param>
            public Entry(KValueInfo value)
            {
                Value = value;
            }

        }

        static readonly TimeSpan DefaultTimeToLive = TimeSpan.FromMinutes(15);

        readonly IKEndpointInvoker<TKNodeId> invoker;
        readonly IKLookup<TKNodeId> lookup;
        readonly IKStore<TKNodeId> store;
        readonly TimeSpan frequency;
        readonly ILogger logger;
        readonly AsyncLock sync = new AsyncLock();
        readonly ConcurrentDictionary<TKNodeId, Entry> entries = new ConcurrentDictionary<TKNodeId, Entry>();

        CancellationTokenSource runCts;
        Task run;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="invoker"></param>
        /// <param name="lookup"></param>
        /// <param name="store"></param>
        /// <param name="frequency"></param>
        /// <param name="logger"></param>
        public KInMemoryPublisher(IKEndpointInvoker<TKNodeId> invoker, IKLookup<TKNodeId> lookup, IKStore<TKNodeId> store, TimeSpan? frequency = null, ILogger logger = null)
        {
            this.invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
            this.lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.frequency = frequency ?? TimeSpan.FromHours(1);
            this.logger = logger;
        }

        /// <summary>
        /// Configures the given value with the given key to be periodically published. If the value is <c>null</c>, it is removed from the publisher.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public ValueTask<bool> AddAsync(in TKNodeId key, in KValueInfo value, CancellationToken cancellationToken = default)
        {
            return AddAsync(key, value, cancellationToken);
        }

        async ValueTask<bool> AddAsync(TKNodeId key, KValueInfo value, CancellationToken cancellationToken)
        {
            using (await sync.LockAsync())
            {
                // must explicitly remove if required
                if (entries.ContainsKey(key))
                    return false;

                // generate new entry
                var entry = new Entry(value);
                entries[key] = entry;

                // publishes the value immediately
                await PublishValueAsync(key, value, cancellationToken);

                return true;
            }
        }

        /// <summary>
        /// Removes the specified key from the publisher.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<bool> RemoveAsync(in TKNodeId key, CancellationToken cancellationToken = default)
        {
            return RemoveAsync(key, cancellationToken);
        }

        async ValueTask<bool> RemoveAsync(TKNodeId key, CancellationToken cancellationToken)
        {
            using (await sync.LockAsync())
                return entries.TryRemove(key, out var _);
        }

        /// <summary>
        /// Gets the value with the given key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KValueInfo?> GetAsync(in TKNodeId key, CancellationToken cancellationToken = default)
        {
            return GetAsync(key, cancellationToken);
        }

        async ValueTask<KValueInfo?> GetAsync(TKNodeId key, CancellationToken cancellationToken)
        {
            using (await sync.LockAsync())
                return entries.TryGetValue(key, out var v) ? v.Value : (KValueInfo?)null;
        }

        /// <summary>
        /// Starts the processes of the publisher.
        /// </summary>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            using (await sync.LockAsync(cancellationToken))
            {
                if (run != null || runCts != null)
                    throw new InvalidOperationException();

                // begin new run processes
                runCts = new CancellationTokenSource();
                run = Task.WhenAll(Task.Run(() => PublishRunAsync(runCts.Token)));
            }
        }

        /// <summary>
        /// Stops the processes of the engine.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            using (await sync.LockAsync(cancellationToken))
            {
                if (runCts != null)
                {
                    runCts.Cancel();
                    runCts = null;
                }

                if (run != null)
                {
                    try
                    {
                        await run;
                    }
                    catch (OperationCanceledException)
                    {
                        // ignore
                    }
                }
            }
        }

        /// <summary>
        /// Publishes the given value to the K closest nodes.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiration"></param>
        /// <param name="version"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task PublishValueAsync(TKNodeId key, KValueInfo value, CancellationToken cancellationToken)
        {
            logger?.LogInformation("Publishing key {Key} with expiration of {Expiration}.", key, value.Expiration);

            // cache in local store
            var s = await store.SetAsync(key, KStoreValueMode.Replica, value);

            // publish to top K remote nodes
            var r = await lookup.LookupNodeAsync(key, cancellationToken);
            var t = r.Nodes.Select(i => invoker.StoreAsync(i.Endpoints, key, KStoreRequestMode.Primary, value, cancellationToken).AsTask());
            await Task.WhenAll(t);
        }

        /// <summary>
        /// Periodically publishes key/value pairs to the appropriate nodes.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task PublishRunAsync(CancellationToken cancellationToken)
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                try
                {
                    logger?.LogInformation("Initiating periodic publish of values.");
                    await Task.WhenAll(entries.Select(i => PublishValueAsync(i.Key, i.Value.Value, cancellationToken)));
                }
                catch (Exception e)
                {
                    logger?.LogError(e, "Unexpected exception occurred publishing values.");
                }

                await Task.Delay(frequency, cancellationToken);
            }
        }

    }

}
