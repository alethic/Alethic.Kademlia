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
        struct Entry
        {

            public byte[] Value;
            public DateTimeOffset Expiration;

            /// <summary>
            /// Initializes a new instance.
            /// </summary>
            /// <param name="value"></param>
            /// <param name="expiration"></param>
            public Entry(byte[] value, DateTimeOffset expiration)
            {
                Value = value;
                Expiration = expiration;
            }

        }

        static readonly TimeSpan DefaultTimeToLive = TimeSpan.FromMinutes(15);

        readonly IKEndpointInvoker<TKNodeId> invoker;
        readonly IKLookup<TKNodeId> lookup;
        readonly IKStore<TKNodeId> store;
        readonly TimeSpan defaultTimeToLive;
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
        /// <param name="defaultTimeToLive"></param>
        /// <param name="logger"></param>
        public KInMemoryPublisher(IKEndpointInvoker<TKNodeId> invoker, IKLookup<TKNodeId> lookup, IKStore<TKNodeId> store, TimeSpan? defaultTimeToLive = null, ILogger logger = null)
        {
            this.invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
            this.lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.defaultTimeToLive = defaultTimeToLive ?? DefaultTimeToLive;
            this.logger = logger;
        }

        /// <summary>
        /// Publishes the given value to the K closest nodes.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiration"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task PublishValueAsync(TKNodeId key, ReadOnlyMemory<byte> value, DateTimeOffset expiration, CancellationToken cancellationToken)
        {
            logger?.LogInformation("Publishing key {Key} with expiration of {Expiration}.", key, expiration);

            // store in local store
            var s = await store.SetAsync(key, value, expiration);

            // publish to top K remote nodes
            var r = await lookup.LookupNodeAsync(key, cancellationToken);
            var t = r.Nodes.Select(i => invoker.StoreAsync(i.Endpoints, key, value, expiration, cancellationToken).AsTask());
            await Task.WhenAll(t);
        }

        /// <summary>
        /// Publishers the given value with the given key. If <paramref name="value"/> is null, the key is removed.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiration"></param>
        /// <returns></returns>
        public ValueTask<KPublisherSetResult<TKNodeId>> SetAsync(in TKNodeId key, ReadOnlyMemory<byte>? value, DateTimeOffset? expiration, CancellationToken cancellationToken = default)
        {
            return SetAsync(key, value, expiration, cancellationToken);
        }

        async ValueTask<KPublisherSetResult<TKNodeId>> SetAsync(TKNodeId key, ReadOnlyMemory<byte>? value, DateTimeOffset? expiration, CancellationToken cancellationToken)
        {
            if (value != null)
            {
                expiration = expiration ?? DateTimeOffset.UtcNow.Add(defaultTimeToLive);
                var entry = new Entry(value.Value.ToArray(), expiration.Value);
                entries[key] = entry;

                // publishes the value immediately
                await PublishValueAsync(key, value.Value, expiration.Value, cancellationToken);
            }
            else
            {
                entries.TryRemove(key, out var _);
            }

            return new KPublisherSetResult<TKNodeId>(KPublisherSetResultStatus.Success);
        }

        /// <summary>
        /// Gets the value with the given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public ValueTask<KPublisherGetResult<TKNodeId>> GetAsync(in TKNodeId key, CancellationToken cancellationToken = default)
        {
            if (entries.TryGetValue(key, out var v))
                return new ValueTask<KPublisherGetResult<TKNodeId>>(new KPublisherGetResult<TKNodeId>(v.Value, v.Expiration));
            else
                return new ValueTask<KPublisherGetResult<TKNodeId>>(new KPublisherGetResult<TKNodeId>(null, null));
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
                    await Task.WhenAll(entries.Select(i => PublishValueAsync(i.Key, i.Value.Value, i.Value.Expiration, cancellationToken)));
                }
                catch (Exception e)
                {
                    logger?.LogError(e, "Unexpected exception occurred publishing values.");
                }

                await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
            }
        }

    }

}
