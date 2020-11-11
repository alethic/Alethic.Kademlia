using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Cogito.Threading;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Alethic.Kademlia.InMemory
{

    /// <summary>
    /// Provides a <see cref="IKPublisher{TNodeId}"/> implementation that keeps published items in memory.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public class KInMemoryPublisher<TNodeId> : IKPublisher<TNodeId>, IHostedService
        where TNodeId : unmanaged
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
            public Entry(in KValueInfo value)
            {
                Value = value;
            }

        }

        static readonly TimeSpan DefaultTimeToLive = TimeSpan.FromMinutes(15);

        readonly IKValueAccessor<TNodeId> accessor;
        readonly TimeSpan frequency;
        readonly ILogger logger;
        readonly AsyncLock sync = new AsyncLock();
        readonly ConcurrentDictionary<TNodeId, Entry> entries = new ConcurrentDictionary<TNodeId, Entry>();

        CancellationTokenSource runCts;
        Task run;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="accessor"></param>
        /// <param name="frequency"></param>
        /// <param name="logger"></param>
        public KInMemoryPublisher(IKValueAccessor<TNodeId> accessor, ILogger logger, TimeSpan? frequency = null)
        {
            this.accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.frequency = frequency ?? TimeSpan.FromHours(1);
        }

        /// <summary>
        /// Configures the given value with the given key to be periodically published. If the value is <c>null</c>, it is removed from the publisher.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public ValueTask<bool> AddAsync(in TNodeId key, in KValueInfo value, CancellationToken cancellationToken = default)
        {
            return AddAsync(key, value, cancellationToken);
        }

        async ValueTask<bool> AddAsync(TNodeId key, KValueInfo value, CancellationToken cancellationToken)
        {
#if NETSTANDARD2_1
            await using (await sync.LockAsync(cancellationToken))
#else
            using (await sync.LockAsync(cancellationToken))
#endif
            {
                // must explicitly remove if required
                if (entries.ContainsKey(key))
                    return false;

                // generate new entry
                var entry = new Entry(value);
                entries[key] = entry;
            }

            // publishes the value immediately
            await PublishValueAsync(key, value, cancellationToken);
            return true;
        }

        /// <summary>
        /// Removes the specified key from the publisher.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<bool> RemoveAsync(in TNodeId key, CancellationToken cancellationToken = default)
        {
            return RemoveAsync(key, cancellationToken);
        }

        async ValueTask<bool> RemoveAsync(TNodeId key, CancellationToken cancellationToken)
        {
#if NETSTANDARD2_1
            await using (await sync.LockAsync(cancellationToken))
#else
            using (await sync.LockAsync(cancellationToken))
#endif
                return entries.TryRemove(key, out var _);
        }

        /// <summary>
        /// Gets the value with the given key.
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
#if NETSTANDARD2_1
            await using (await sync.LockAsync(cancellationToken))
#else
            using (await sync.LockAsync(cancellationToken))
#endif
                return entries.TryGetValue(key, out var v) ? v.Value : (KValueInfo?)null;
        }

        /// <summary>
        /// Starts the processes of the publisher.
        /// </summary>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
#if NETSTANDARD2_1
            await using (await sync.LockAsync(cancellationToken))
#else
            using (await sync.LockAsync(cancellationToken))
#endif
            {
                if (run != null || runCts != null)
                    throw new InvalidOperationException();

                // begin new run processes
                runCts = new CancellationTokenSource();
                run = Task.WhenAll(Task.Run(() => PublishRunAsync(runCts.Token)));
            }
        }

        /// <summary>
        /// Stops the processes of the publisher.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
#if NETSTANDARD2_1
            await using (await sync.LockAsync(cancellationToken))
#else
            using (await sync.LockAsync(cancellationToken))
#endif
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
        async Task PublishValueAsync(TNodeId key, KValueInfo value, CancellationToken cancellationToken)
        {
            logger.LogInformation("Publishing key {Key} with expiration of {Expiration}.", key, value.Expiration);
            await accessor.SetAsync(key, value, cancellationToken);
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
                    logger.LogInformation("Initiating periodic publish of values.");
                    await Task.WhenAll(entries.Select(i => PublishValueAsync(i.Key, i.Value.Value, cancellationToken)));
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Unexpected exception occurred publishing values.");
                }

                await Task.Delay(frequency, cancellationToken);
            }
        }

    }

}
