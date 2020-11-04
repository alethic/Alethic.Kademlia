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
    /// <typeparam name="TNodeId"></typeparam>
    public class KEndpointSet<TNodeId> : IKProtocolEndpointSet<TNodeId>, IDisposable
        where TNodeId : unmanaged
    {

        readonly OrderedDictionary<IKProtocolEndpoint<TNodeId>, KEndpointInfo<TNodeId>> dict;
        readonly ReaderWriterLockSlim sync = new ReaderWriterLockSlim();

        /// <summary>
        /// </summary>
        /// Initializes a new instance.
        public KEndpointSet()
        {
            dict = new OrderedDictionary<IKProtocolEndpoint<TNodeId>, KEndpointInfo<TNodeId>>(EqualityComparer<IKProtocolEndpoint<TNodeId>>.Default);
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="source"></param>
        public KEndpointSet(IEnumerable<IKProtocolEndpoint<TNodeId>> source) :
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
        public IKProtocolEndpoint<TNodeId> Acquire()
        {
            using (sync.BeginReadLock())
                return dict.Count > 0 ? dict.First.Key : null;
        }

        bool AddFirst(IKProtocolEndpoint<TNodeId> endpoint, KEndpointInfo<TNodeId> info)
        {
            using (sync.BeginWriteLock())
                return dict.AddFirst(endpoint, info);
        }

        bool AddLast(IKProtocolEndpoint<TNodeId> endpoint, KEndpointInfo<TNodeId> info)
        {
            using (sync.BeginWriteLock())
                return dict.AddLast(endpoint, info);
        }

        public KEndpointInfo<TNodeId> Insert(IKProtocolEndpoint<TNodeId> endpoint)
        {
            using (sync.BeginUpgradableReadLock())
            {
                if (dict.TryGetValue(endpoint, out var info) == false)
                    AddLast(endpoint, info = new KEndpointInfo<TNodeId>(DateTime.MinValue));

                return info;
            }
        }

        public KEndpointInfo<TNodeId> Update(IKProtocolEndpoint<TNodeId> endpoint)
        {
            using (sync.BeginUpgradableReadLock())
            {
                if (dict.TryGetValue(endpoint, out var info))
                    AddFirst(endpoint, info);
                else
                    AddFirst(endpoint, info = new KEndpointInfo<TNodeId>(DateTime.MinValue));

                return info;
            }
        }

        public KEndpointInfo<TNodeId> Demote(IKProtocolEndpoint<TNodeId> endpoint)
        {
            using (sync.BeginUpgradableReadLock())
            {
                if (dict.TryGetValue(endpoint, out var info))
                    AddLast(endpoint, info);
                else
                    AddLast(endpoint, info = new KEndpointInfo<TNodeId>(DateTime.MinValue));

                return info;
            }
        }

        public KEndpointInfo<TNodeId> Select(IKProtocolEndpoint<TNodeId> endpoint)
        {
            using (sync.BeginReadLock())
                return dict.TryGetValue(endpoint, out var info) ? info : null;
        }

        public KEndpointInfo<TNodeId> Remove(IKProtocolEndpoint<TNodeId> endpoint)
        {
            using (sync.BeginUpgradableReadLock())
                if (dict.TryGetValue(endpoint, out var info))
                    using (sync.BeginWriteLock())
                        if (dict.Remove(endpoint))
                            return info;

            return default;
        }

        public IEnumerator<IKProtocolEndpoint<TNodeId>> GetEnumerator()
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
