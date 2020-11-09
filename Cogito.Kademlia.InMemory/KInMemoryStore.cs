using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Cogito.Collections;
using Cogito.Threading;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cogito.Kademlia.InMemory
{

    /// <summary>
    /// Provides a <see cref="IKStore{TNodeId}"/> implementation that uses an <see cref="IMemoryCache"/> instance as a backing store.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public class KInMemoryStore<TNodeId> : IKStore<TNodeId>, IHostedService
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Represents an entry in the cache.
        /// </summary>
        class Entry
        {

            public TNodeId Key;
            public KStoreValueMode Mode;
            public KValueInfo Value;
            public DateTime ExpireTime;
            public DateTime? ReplicateTime;

            /// <summary>
            /// Initializes a new instance.
            /// </summary>
            /// <param name="key"></param>
            /// <param name="mode"></param>
            /// <param name="value"></param>
            /// <param name="expireTime"></param>
            /// <param name="replicateTime"></param>
            public Entry(in TNodeId key, KStoreValueMode mode, KValueInfo value, DateTime expireTime, DateTime? replicateTime)
            {
                Key = key;
                Mode = mode;
                Value = value;
                ExpireTime = expireTime;
                ReplicateTime = replicateTime;
            }

        }

        static readonly TimeSpan DefaultFrequency = TimeSpan.FromSeconds(5);

        readonly IKHost<TNodeId> host;
        readonly IKRouter<TNodeId> router;
        readonly IKInvoker<TNodeId> invoker;
        readonly IKLookup<TNodeId> lookup;
        readonly TimeSpan frequency;
        readonly ILogger logger;
        readonly C5.IntervalHeap<Entry> delQueue;
        readonly C5.IntervalHeap<Entry> repQueue;
        readonly Dictionary<TNodeId, (Entry Entry, C5.IPriorityQueueHandle<Entry> DelQueueHandle, C5.IPriorityQueueHandle<Entry> RepQueueHandle)> entries;
        readonly AsyncLock sync = new AsyncLock();
        readonly ReaderWriterLockSlim slim = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        CancellationTokenSource runCts;
        Task run;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="router"></param>
        /// <param name="invoker"></param>
        /// <param name="lookup"></param>
        /// <param name="logger"></param>
        /// <param name="frequency"></param>
        public KInMemoryStore(IKHost<TNodeId> host, IKRouter<TNodeId> router, IKInvoker<TNodeId> invoker, IKLookup<TNodeId> lookup, ILogger logger, TimeSpan? frequency = null)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
            this.router = router ?? throw new ArgumentNullException(nameof(router));
            this.invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
            this.lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.frequency = frequency ?? DefaultFrequency;

            this.delQueue = new C5.IntervalHeap<Entry>(32, new FuncComparer<Entry, DateTime>(e => e.ExpireTime, Comparer<DateTime>.Default));
            this.repQueue = new C5.IntervalHeap<Entry>(32, new FuncComparer<Entry, DateTime>(e => e.ReplicateTime.Value, Comparer<DateTime>.Default));
            this.entries = new Dictionary<TNodeId, (Entry, C5.IPriorityQueueHandle<Entry>, C5.IPriorityQueueHandle<Entry>)>(32);
        }

        /// <summary>
        /// Stores the given value with the given key. If <paramref name="value"/> is null, the key is removed.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="mode"></param>
        /// <param name="value"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<bool> SetAsync(in TNodeId key, KStoreValueMode mode, in KValueInfo? value, CancellationToken cancellationToken = default)
        {
            return SetAsync(key, mode, value, cancellationToken);
        }

        async ValueTask<bool> SetAsync(TNodeId key, KStoreValueMode mode, KValueInfo? value, CancellationToken cancellationToken)
        {
            // do some work before entering lock
            var now = DateTime.UtcNow;
            var expireTime = await CalculateExpireTime(key, mode, value.Value.Expiration, cancellationToken);

            using (slim.BeginUpgradableReadLock())
            {
                if (value != null)
                {
                    C5.IPriorityQueueHandle<Entry> delQueueHandle = null;
                    C5.IPriorityQueueHandle<Entry> repQueueHandle = null;

                    // value already exists, but version is lower
                    if (entries.TryGetValue(key, out var record))
                    {
                        // save existing handles
                        delQueueHandle = record.DelQueueHandle;
                        repQueueHandle = record.RepQueueHandle;

                        // record already exists but with a greater version, we will not replace
                        if (record.Entry.Value.Version >= value.Value.Version && record.Entry.Mode <= mode)
                        {
                            logger.LogInformation("Ignoring value for {Key}: Verison {OldVersion} >= {NewVersion}.", key, record.Entry.Value.Version, value.Value.Version);
                            return false;
                        }

                        // will end up refreshing in queues
                        using (slim.BeginWriteLock())
                        {
                            // remove from del queue
                            if (delQueueHandle != null && delQueue.Find(delQueueHandle, out _))
                                delQueue.Delete(delQueueHandle);

                            // remove from rep queue
                            if (repQueueHandle != null && repQueue.Find(repQueueHandle, out _))
                                repQueue.Delete(repQueueHandle);
                        }
                    }

                    using (slim.BeginWriteLock())
                    {
                        var replicateTime = mode == KStoreValueMode.Primary ? now + frequency : (DateTime?)null;

                        logger.LogInformation("Storing {Key} as {Mode} in memory store with expiration at {ExpireTime}.", key, mode, expireTime);
                        var entry = new Entry(key, mode, value.Value, expireTime, replicateTime);

                        // add to del queue
                        delQueue.Add(ref delQueueHandle, entry);

                        // add to rep queue if will replicate
                        if (replicateTime != null)
                            repQueue.Add(ref repQueueHandle, entry);

                        // map entry
                        entries[key] = (entry, delQueueHandle, repQueueHandle);

                        OnValueChanged(new KValueEventArgs<TNodeId>(key, value));
                    }
                }
                else if (entries.TryGetValue(key, out var record))
                {
                    using (slim.BeginWriteLock())
                    {
                        logger.LogInformation("Removing {Key} from memory store.", key);

                        // remove from del queue
                        if (record.DelQueueHandle != null && delQueue.Find(record.DelQueueHandle, out _))
                            delQueue.Delete(record.DelQueueHandle);

                        // remove from rep queue
                        if (record.RepQueueHandle != null && repQueue.Find(record.RepQueueHandle, out _))
                            repQueue.Delete(record.RepQueueHandle);

                        // remove entry
                        entries.Remove(key);
                    }

                    OnValueChanged(new KValueEventArgs<TNodeId>(key, null));
                }

                return true;
            }
        }

        /// <summary>
        /// Calculates the locate expire time from the passed absolute expiration date.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="expiration"></param>
        /// <returns></returns>
        async ValueTask<DateTime> CalculateExpireTime(TNodeId key, KStoreValueMode mode, DateTime expiration, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            // primary targets expire at standard offset; as do expired values
            if (mode == KStoreValueMode.Primary || expiration <= now)
                return expiration;

            // calculation derived from https://www.syncfusion.com/ebooks/kademlia_protocol_succinctly/key-value-management
            var d = new KNodeIdDistanceComparer<TNodeId>(host.SelfId);
#if NETSTANDARD2_0
            var l = await router.SelectAsync(key, 1024, cancellationToken);
            var c = l.Count(i => d.Compare(key, i.Id) > 0);
#else
            var l = router.SelectAsync(key, 1024, cancellationToken);
            var c = await l.CountAsync(i => d.Compare(key, i.Id) > 0);
#endif
            var t = (int)(expiration - now).TotalSeconds;
            var o = t / System.Math.Pow(2, c);
            return now.AddSeconds(o);
        }

        /// <summary>
        /// Gets the value with the given key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KValueInfo?> GetAsync(in TNodeId key, CancellationToken cancellationToken = default)
        {
            using (slim.BeginReadLock())
            {
                if (entries.TryGetValue(key, out var v) && v.Entry.Value.Expiration > DateTime.UtcNow)
                    return new ValueTask<KValueInfo?>(v.Entry.Value);
                else
                    return new ValueTask<KValueInfo?>((KValueInfo?)null);
            }
        }

        /// <summary>
        /// Starts the processes of the store.
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
                run = Task.WhenAll(
                    Task.Run(() => ExpireRunAsync(runCts.Token)),
                    Task.Run(() => ReplicateRunAsync(runCts.Token)));
            }
        }

        /// <summary>
        /// Stops the processes of the store.
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
        /// Periodically removes expired values.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task ExpireRunAsync(CancellationToken cancellationToken)
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                try
                {
                    using (slim.BeginUpgradableReadLock())
                    {
                        // continue while the first item is expired
                        while (
                            delQueue.Count > 0 &&
                            delQueue.FindMin() is Entry entry &&
                            entry.ExpireTime <= DateTime.UtcNow &&
                            entries.TryGetValue(entry.Key, out var record))
                        {
                            using (slim.BeginWriteLock())
                            {
                                logger.LogInformation("Removing expired key {Key}.", entry.Key);

                                // remove from del queue
                                if (record.DelQueueHandle != null && delQueue.Find(record.DelQueueHandle, out _))
                                    delQueue.Delete(record.DelQueueHandle);

                                // remove from rep queue
                                if (record.RepQueueHandle != null && repQueue.Find(record.RepQueueHandle, out _))
                                    repQueue.Delete(record.RepQueueHandle);

                                // remove entry
                                entries.Remove(entry.Key);
                            }

                            OnValueChanged(new KValueEventArgs<TNodeId>(entry.Key, null));
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Unexpected exception occurred republishing stored values.");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }

        /// <summary>
        /// Periodically publishes key/value pairs to the appropriate nodes.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task ReplicateRunAsync(CancellationToken cancellationToken)
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                try
                {
                    List<Task> l = null;

                    using (slim.BeginUpgradableReadLock())
                    {
                        // continue while the first item is expired
                        while (
                            repQueue.Count > 0 &&
                            repQueue.FindMin() is Entry entry &&
                            entry.ReplicateTime != null &&
                            entry.ReplicateTime <= DateTime.UtcNow &&
                            entries.TryGetValue(entry.Key, out var record))
                        {
                            if (l == null)
                                l = new List<Task>();

                            using (slim.BeginWriteLock())
                            {
                                // schedule replication
                                l.Add(Task.Run(() => ReplicateAsync(entry, cancellationToken)));

                                // update to next time
                                entry.ReplicateTime = DateTime.UtcNow + frequency;

                                // remove existing queue entry
                                if (record.RepQueueHandle != null)
                                    repQueue.Delete(record.RepQueueHandle);

                                // add new queue entry
                                repQueue.Add(ref record.RepQueueHandle, entry);
                            }
                        }
                    }

                    // wait for all our replicate events to finish
                    if (l != null)
                        await Task.WhenAll(l);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Unexpected exception occurred republishing stored values.");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
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
        async Task ReplicateAsync(Entry entry, CancellationToken cancellationToken)
        {
            logger.LogInformation("Replicating key {Key} with expiration of {Expiration}.", entry.Key, entry.Value.Expiration);

            // publish to top K remote nodes
            var r = await lookup.LookupNodeAsync(entry.Key, cancellationToken);
            var t = r.Nodes.Select(i => invoker.StoreAsync(i.Endpoints, entry.Key, KStoreRequestMode.Replica, entry.Value, cancellationToken).AsTask());
            await Task.WhenAll(t);
        }

        /// <summary>
        /// Raised when a value is changed.
        /// </summary>
        public event EventHandler<KValueEventArgs<TNodeId>> ValueChanged;

        /// <summary>
        /// Raises the ValueChanged event.
        /// </summary>
        /// <param name="args"></param>
        void OnValueChanged(KValueEventArgs<TNodeId> args)
        {
            ValueChanged?.Invoke(this, args);
        }

    }

}
