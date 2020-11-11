using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;

using Cogito.Collections;
using Cogito.Threading;

namespace Alethic.Kademlia
{

    /// <summary>
    /// Tracks a set of endpoints, managing their position within the set based on their timeout or success events.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public class KProtocolEndpointSet<TNodeId> : IDisposable, ICollection<IKProtocolEndpoint<TNodeId>>, INotifyCollectionChanged
        where TNodeId : unmanaged
    {

        readonly OrderedSet<IKProtocolEndpoint<TNodeId>> set = new OrderedSet<IKProtocolEndpoint<TNodeId>>(EqualityComparer<IKProtocolEndpoint<TNodeId>>.Default);
        readonly ReaderWriterLockSlim sync = new ReaderWriterLockSlim();

        /// <summary>
        /// </summary>
        /// Initializes a new instance.
        public KProtocolEndpointSet()
        {

        }

        /// <summary>
        /// Raised when an item is added or removed from the collection.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Raises the CollectionChanged event.
        /// </summary>
        /// <param name="args"></param>
        void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            CollectionChanged?.Invoke(this, args);
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="source"></param>
        public KProtocolEndpointSet(IEnumerable<IKProtocolEndpoint<TNodeId>> source) :
            this()
        {
            // add source endpoints to set
            foreach (var i in source)
                Insert(i);
        }

        /// <summary>
        /// Gets whether or not the collection is read only.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Gets the number of items in the set.
        /// </summary>
        public int Count
        {
            get
            {
                using (sync.BeginReadLock())
                    return ((ICollection<IKProtocolEndpoint<TNodeId>>)set).Count;
            }
        }

        /// <summary>
        /// Returns <c>true</c> if the set contains this endpoint.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(IKProtocolEndpoint<TNodeId> item)
        {
            using (sync.BeginReadLock())
                return set.Contains(item);
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
            var b = false;
            using (sync.BeginWriteLock())
                b = set.AddLast(endpoint);

            if (b)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, endpoint));
        }

        public void Update(IKProtocolEndpoint<TNodeId> endpoint)
        {
            var b = false;

            using (sync.BeginWriteLock())
                b = set.AddFirst(endpoint);

            if (b)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, endpoint));
        }

        public bool Remove(IKProtocolEndpoint<TNodeId> endpoint)
        {
            var b = false;

            using (sync.BeginUpgradableReadLock())
                if (set.Contains(endpoint))
                    using (sync.BeginWriteLock())
                        b = set.Remove(endpoint);

            if (b)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, endpoint));

            return b;
        }

        public void Clear()
        {
            var b = set.Count > 0;
            set.Clear();

            if (b)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public IEnumerator<IKProtocolEndpoint<TNodeId>> GetEnumerator()
        {
            using (sync.BeginReadLock())
                return new List<IKProtocolEndpoint<TNodeId>>(set).GetEnumerator();
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

        void ICollection<IKProtocolEndpoint<TNodeId>>.Add(IKProtocolEndpoint<TNodeId> item)
        {
            Insert(item);
        }

        void ICollection<IKProtocolEndpoint<TNodeId>>.CopyTo(IKProtocolEndpoint<TNodeId>[] array, int arrayIndex)
        {
            using (sync.BeginReadLock())
                set.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Finalizes the instance.
        /// </summary>
        ~KProtocolEndpointSet()
        {
            Dispose();
        }

    }

}
