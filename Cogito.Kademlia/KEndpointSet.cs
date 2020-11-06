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
    public class KEndpointSet<TNodeId> : IDisposable, IEnumerable<IKProtocolEndpoint<TNodeId>>
        where TNodeId : unmanaged
    {

        readonly OrderedSet<IKProtocolEndpoint<TNodeId>> set;
        readonly ReaderWriterLockSlim sync = new ReaderWriterLockSlim();

        /// <summary>
        /// </summary>
        /// Initializes a new instance.
        public KEndpointSet()
        {
            set = new OrderedSet<IKProtocolEndpoint<TNodeId>>(EqualityComparer<IKProtocolEndpoint<TNodeId>>.Default);
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
                return set.Count > 0 ? set.First : null;
        }

        public void Insert(IKProtocolEndpoint<TNodeId> endpoint)
        {
            using (sync.BeginWriteLock())
                set.AddLast(endpoint);
        }

        public void Update(IKProtocolEndpoint<TNodeId> endpoint)
        {
            using (sync.BeginWriteLock())
                set.AddFirst(endpoint);
        }

        public bool Remove(IKProtocolEndpoint<TNodeId> endpoint)
        {
            using (sync.BeginUpgradableReadLock())
                if (set.Contains(endpoint))
                    using (sync.BeginWriteLock())
                        return set.Remove(endpoint);

            return false;
        }

        public IEnumerator<IKProtocolEndpoint<TNodeId>> GetEnumerator()
        {
            using (sync.BeginReadLock())
                return set.ToList().GetEnumerator();
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
                set.Clear();

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
