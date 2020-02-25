using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Provides a <see cref="IKStore{TKNodeId}"/> implementation that uses an <see cref="IMemoryCache"/> instance as a backing store.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public class KInMemoryStore<TKNodeId> : IKStore<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Represents an entry in the cache.
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

        readonly IMemoryCache cache;
        readonly TimeSpan defaultTimeToLive;
        readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="defaultTimeToLive"></param>
        /// <param name="logger"></param>
        public KInMemoryStore(IMemoryCache cache, TimeSpan? defaultTimeToLive = null, ILogger logger = null)
        {
            this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
            this.defaultTimeToLive = defaultTimeToLive ?? DefaultTimeToLive;
            this.logger = logger;
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public KInMemoryStore(TimeSpan? defaultTimeToLive = null, ILogger logger = null) :
            this(new MemoryCache(Options.Create(new MemoryCacheOptions())), defaultTimeToLive, logger)
        {

        }

        /// <summary>
        /// Stores the given value with the given key. If <paramref name="value"/> is null, the key is removed.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiration"></param>
        /// <returns></returns>
        public ValueTask<KStoreSetResult<TKNodeId>> SetAsync(in TKNodeId key, ReadOnlyMemory<byte>? value, DateTimeOffset? expiration)
        {
            if (value != null)
            {
                expiration = expiration ?? DateTimeOffset.UtcNow.Add(defaultTimeToLive);
                logger?.LogInformation("Storing {Key} in memory cache with expiration at {Expiration}.", key, expiration);
                cache.Set(key, new Entry(value.Value.ToArray(), expiration.Value), expiration.Value);
            }
            else
            {
                logger?.LogInformation("Removing {Key} in memory cache.", key, expiration);
                cache.Remove(key);
            }

            return new ValueTask<KStoreSetResult<TKNodeId>>(new KStoreSetResult<TKNodeId>(KStoreSetResultStatus.Success));
        }

        /// <summary>
        /// Gets the value with the given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public ValueTask<KStoreGetResult<TKNodeId>> GetAsync(in TKNodeId key)
        {
            if (cache.TryGetValue<Entry>(key, out var v))
                return new ValueTask<KStoreGetResult<TKNodeId>>(new KStoreGetResult<TKNodeId>(v.Value, v.Expiration));
            else
                return new ValueTask<KStoreGetResult<TKNodeId>>(new KStoreGetResult<TKNodeId>(null, null));
        }

    }

}
