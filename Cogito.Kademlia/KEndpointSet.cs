using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Cogito.Collections;
using Cogito.Threading;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Tracks a set of endpoints, managing their position within the set based on their timeout or success events.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public class KEndpointSet<TKNodeId> : IKEndpointSet<TKNodeId>, IDisposable
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly OrderedDictionary<IKEndpoint<TKNodeId>, KEndpointInfo<TKNodeId>> dict;
        readonly ReaderWriterLockSlim sync = new ReaderWriterLockSlim();

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public KEndpointSet()
        {
            dict = new OrderedDictionary<IKEndpoint<TKNodeId>, KEndpointInfo<TKNodeId>>(EqualityComparer<IKEndpoint<TKNodeId>>.Default);
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="source"></param>
        public KEndpointSet(IEnumerable<IKEndpoint<TKNodeId>> source) :
            this()
        {
            // add source endpoints to set
            foreach (var i in source)
                Insert(i);
        }

        /// <summary>
        /// Acquires the first available endpoint.
        /// </summary>
        /// <returns></returns>
        public IKEndpoint<TKNodeId> Acquire()
        {
            using (sync.BeginReadLock())
                return dict.Count > 0 ? dict.First.Key : null;
        }

        bool AddFirst(IKEndpoint<TKNodeId> endpoint, KEndpointInfo<TKNodeId> info)
        {
            using (sync.BeginWriteLock())
                return dict.AddFirst(endpoint, info);
        }

        bool AddLast(IKEndpoint<TKNodeId> endpoint, KEndpointInfo<TKNodeId> info)
        {
            using (sync.BeginWriteLock())
                return dict.AddLast(endpoint, info);
        }

        public KEndpointInfo<TKNodeId> Insert(IKEndpoint<TKNodeId> endpoint)
        {
            using (sync.BeginUpgradableReadLock())
            {
                if (dict.TryGetValue(endpoint, out var info) == false)
                    AddLast(endpoint, info = new KEndpointInfo<TKNodeId>(DateTime.MinValue));

                return info;
            }
        }

        public KEndpointInfo<TKNodeId> Update(IKEndpoint<TKNodeId> endpoint)
        {
            using (sync.BeginUpgradableReadLock())
            {
                if (dict.TryGetValue(endpoint, out var info))
                    AddFirst(endpoint, info);
                else
                    AddFirst(endpoint, info = new KEndpointInfo<TKNodeId>(DateTime.MinValue));

                return info;
            }
        }

        public KEndpointInfo<TKNodeId> Demote(IKEndpoint<TKNodeId> endpoint)
        {
            using (sync.BeginUpgradableReadLock())
            {
                if (dict.TryGetValue(endpoint, out var info))
                    AddLast(endpoint, info);
                else
                    AddLast(endpoint, info = new KEndpointInfo<TKNodeId>(DateTime.MinValue));

                return info;
            }
        }

        public KEndpointInfo<TKNodeId> Select(IKEndpoint<TKNodeId> endpoint)
        {
            using (sync.BeginReadLock())
                return dict.TryGetValue(endpoint, out var info) ? info : null;
        }

        public KEndpointInfo<TKNodeId> Remove(IKEndpoint<TKNodeId> endpoint)
        {
            using (sync.BeginUpgradableReadLock())
                if (dict.TryGetValue(endpoint, out var info))
                    using (sync.BeginWriteLock())
                        if (dict.Remove(endpoint))
                            return info;

            return default;
        }

        public IEnumerator<IKEndpoint<TKNodeId>> GetEnumerator()
        {
            using (sync.BeginReadLock())
                return dict.Select(i => i.Key).ToList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Dispose of the instance.
        /// </summary>
        public void Dispose()
        {
            using (sync.BeginWriteLock())
                dict.Clear();

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizes the instance.
        /// </summary>
        ~KEndpointSet()
        {
            Dispose();
        }

    }

}
