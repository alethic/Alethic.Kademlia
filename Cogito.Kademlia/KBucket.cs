using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Cogito.Collections;
using Cogito.Threading;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Represents a set of items within a <see cref="KTable{TKNodeId, TKBucketItem}"/>.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    /// <typeparam name="TKPeerData"></typeparam>
    class KBucket<TKNodeId, TKPeerData> : IEnumerable<KBucketItem<TKNodeId, TKPeerData>>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
        where TKPeerData : IKEndpointProvider<TKNodeId>, new()
    {

        readonly int k;
        readonly ReaderWriterLockSlim rw = new ReaderWriterLockSlim();
        readonly LinkedList<KBucketItem<TKNodeId, TKPeerData>> l;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="k"></param>
        public KBucket(int k)
        {
            this.k = k;

            l = new LinkedList<KBucketItem<TKNodeId, TKPeerData>>();
        }

        /// <summary>
        /// Attempts to ping all of the provided endpoints and returns <c>true</c> if any are reachable.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<IEnumerable<IKEndpoint<TKNodeId>>> TryPingAsync(IKEndpoint<TKNodeId> endpoint, CancellationToken cancellationToken)
        {
            try
            {
                var r = await endpoint.PingAsync(new KPingRequest<TKNodeId>(), cancellationToken);
                if (r.Body.Endpoints.Length > 0)
                    return r.Body.Endpoints.ToArray();
            }
            catch (OperationCanceledException)
            {
                // ignore
            }

            return null;
        }

        /// <summary>
        /// Attempts to ping all of the provided endpoints and returns <c>true</c> if any are reachable.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<IEnumerable<IKEndpoint<TKNodeId>>> TryPingAsync(IEnumerable<IKEndpoint<TKNodeId>> endpoints, CancellationToken cancellationToken)
        {
            foreach (var endpoint in endpoints)
                if (await TryPingAsync(endpoint, cancellationToken) is IEnumerable<IKEndpoint<TKNodeId>> e)
                    return e;

            return null;
        }

        /// <summary>
        /// Updates the given node with the newly available endpoints.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="endpoints"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal ValueTask<TKPeerData> GetPeerAsync(in TKNodeId nodeId, CancellationToken cancellationToken)
        {
            var n = GetNode(nodeId);
            if (n != null)
                return new ValueTask<TKPeerData>(n.Value.Data);
            else
                return default;
        }

        /// <summary>
        /// Updates the given node with the newly available endpoints.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="endpoints"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal ValueTask UpdatePeerAsync(in TKNodeId nodeId, IEnumerable<IKEndpoint<TKNodeId>> endpoints, CancellationToken cancellationToken)
        {
            return UpdatePeerAsync(nodeId, endpoints, cancellationToken);
        }

        /// <summary>
        /// Updates the given node with the newly available endpoints.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="endpoints"></param>
        /// <param name="cancellationToken"></param>
        async ValueTask UpdatePeerAsync(TKNodeId nodeId, IEnumerable<IKEndpoint<TKNodeId>> endpoints, CancellationToken cancellationToken)
        {
            if (endpoints is null)
                throw new ArgumentNullException(nameof(endpoints));

            var lk = rw.BeginUpgradableReadLock();

            try
            {
                var i = GetNode(nodeId);
                if (i != null)
                {
                    using (rw.BeginWriteLock())
                    {
                        // item already exists, move to tail
                        l.Remove(i);
                        l.AddLast(i);

                        // update endpoints
                        if (endpoints != null)
                            i.Value.Data.Endpoints.AddRange(endpoints);
                    }
                }
                else if (l.Count < k)
                {
                    using (rw.BeginWriteLock())
                    {
                        // generate new peer entry
                        var p = new TKPeerData();
                        if (endpoints != null)
                            p.Endpoints.AddRange(endpoints);

                        // item does not exist, but bucket has room, insert at tail
                        l.AddLast(new KBucketItem<TKNodeId, TKPeerData>(nodeId, p));
                    }
                }
                else
                {
                    // item does not exist, but bucket does not have room, ping first entry
                    var n = l.First;

                    // start ping, check for async completion
                    var r = (IEnumerable<IKEndpoint<TKNodeId>>)null;
                    var t = TryPingAsync(n.Value.Data.Endpoints, cancellationToken);

                    // completed synchronously (or immediately)
                    if (t.IsCompleted)
                        r = t.Result;
                    else
                    {
                        // temporarily release lock and wait for completion
                        lk.Dispose();
                        r = await t;
                        lk = rw.BeginUpgradableReadLock();
                    }

                    // was able to successfully ping the node
                    if (r != null)
                    {
                        // entry had response, move to tail, discard new entry
                        if (l.Count > 1)
                        {
                            using (rw.BeginWriteLock())
                            {
                                // remove from list if not already done (async operation could have overlapped)
                                if (n.List != null)
                                    l.Remove(n);

                                // node goes to end
                                l.AddLast(n.Value);
                            }
                        }
                    }
                    else
                    {
                        using (rw.BeginWriteLock())
                        {
                            // first entry had no response, remove, insert new at tail
                            l.Remove(n);
                            var p = new TKPeerData();
                            p.Endpoints.AddRange(endpoints);
                            l.AddLast(new KBucketItem<TKNodeId, TKPeerData>(nodeId, p));
                        }
                    }
                }
            }
            finally
            {
                lk.Dispose();
            }
        }

        /// <summary>
        /// Finds the current index of the specified node ID within the bucket.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        LinkedListNode<KBucketItem<TKNodeId, TKPeerData>> GetNode(in TKNodeId nodeId)
        {
            using (rw.BeginReadLock())
                for (var i = l.First; i != null; i = i.Next)
                    if (nodeId.Equals(i.Value.Id))
                        return i;

            return null;
        }

        /// <summary>
        /// Returns the number of items within the bucket.
        /// </summary>
        public int Count
        {
            get
            {
                using (rw.BeginReadLock())
                    return l.Count;
            }
        }

        /// <summary>
        /// Gets an iterator that covers a snapshot of the bucket items.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KBucketItem<TKNodeId, TKPeerData>> GetEnumerator()
        {
            using (rw.BeginReadLock())
                return l.ToList().GetEnumerator();
        }

        /// <summary>
        /// Gets an iterator that covers a snapshot of the bucket items.
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }

}
