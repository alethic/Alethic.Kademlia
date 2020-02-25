using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Cogito.Collections;
using Cogito.Threading;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Defines a standard list of endpoints.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public class KEndpointSet<TKNodeId> : OrderedDictionary<IKEndpoint<TKNodeId>, KEndpointInfo<TKNodeId>>, IKEndpointSet<TKNodeId>, IDisposable
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly ReaderWriterLockSlim sync = new ReaderWriterLockSlim();

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public KEndpointSet() :
            base(EqualityComparer<IKEndpoint<TKNodeId>>.Default)
        {

        }

        void Endpoint_Timeout(object sender, KEndpointTimeoutEventArgs args)
        {
            Demote((IKEndpoint<TKNodeId>)sender);
        }

        new bool AddFirst(IKEndpoint<TKNodeId> endpoint, KEndpointInfo<TKNodeId> info)
        {
            if (base.AddFirst(endpoint, info))
            {
                endpoint.Timeout += Endpoint_Timeout;
                return true;
            }

            return false;
        }

        new bool AddLast(IKEndpoint<TKNodeId> endpoint, KEndpointInfo<TKNodeId> info)
        {
            if (base.AddLast(endpoint, info))
            {
                endpoint.Timeout += Endpoint_Timeout;
                return true;
            }

            return false;
        }

        new bool Remove(IKEndpoint<TKNodeId> endpoint)
        {
            if (base.Remove(endpoint))
            {
                endpoint.Timeout -= Endpoint_Timeout;
                return true;
            }

            return false;
        }

        public KEndpointInfo<TKNodeId> Update(IKEndpoint<TKNodeId> endpoint)
        {
            using (sync.BeginWriteLock())
            {
                if (TryGetValue(endpoint, out var info))
                    AddFirst(endpoint, info);
                else
                    AddFirst(endpoint, info = new KEndpointInfo<TKNodeId>(DateTime.MinValue));

                return info;
            }
        }

        public KEndpointInfo<TKNodeId> Demote(IKEndpoint<TKNodeId> endpoint)
        {
            using (sync.BeginWriteLock())
            {
                if (TryGetValue(endpoint, out var info))
                    AddLast(endpoint, info);
                else
                    AddLast(endpoint, info = new KEndpointInfo<TKNodeId>(DateTime.MinValue));

                return info;
            }
        }

        public KEndpointInfo<TKNodeId> Select(IKEndpoint<TKNodeId> endpoint)
        {
            using (sync.BeginReadLock())
                return TryGetValue(endpoint, out var info) ? info : null;
        }

        KEndpointInfo<TKNodeId> IKEndpointSet<TKNodeId>.Remove(IKEndpoint<TKNodeId> endpoint)
        {
            using (sync.BeginUpgradableReadLock())
                return TryGetValue(endpoint, out var info) && Remove(endpoint) ? info : null;
        }

        IEnumerator<IKEndpoint<TKNodeId>> IEnumerable<IKEndpoint<TKNodeId>>.GetEnumerator()
        {
            using (sync.BeginReadLock())
                return Keys.ToList().GetEnumerator();
        }

        /// <summary>
        /// Dispose of the instance.
        /// </summary>
        public void Dispose()
        {
            using (sync.BeginWriteLock())
                foreach (var item in this)
                    item.Key.Timeout -= Endpoint_Timeout;

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
