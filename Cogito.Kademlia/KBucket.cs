using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Cogito.Threading;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Represents a set of items within a <see cref="KTable{TKNodeId, TKBucketItem}"/>.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    /// <typeparam name="TKPeerData"></typeparam>
    class KBucket<TKNodeId, TKPeerData>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
        where TKPeerData : IKEndpointProvider<TKNodeId>
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
        async ValueTask<bool> TryPingAsync(IEnumerable<IKEndpoint<TKNodeId>> endpoints, CancellationToken cancellationToken)
        {
            foreach (var endpoint in endpoints)
            {
                var r = await endpoint.PingAsync(new KPingRequest<TKNodeId>(), cancellationToken);
                if (r.Status == KResponseStatus.OK)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Updates the position of the node in the bucket.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="peerData"></param>
        /// <param name="peerEvents"></param>
        /// <param name="cancellationToken"></param>
        internal async ValueTask TouchAsync(TKNodeId nodeId, TKPeerData peerData, CancellationToken cancellationToken)
        {
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
                        l.AddLast(i.Value);
                    }
                }
                else if (l.Count < k)
                {
                    using (rw.BeginWriteLock())
                    {
                        // item does not exist, but bucket has room, insert at tail
                        l.AddLast(new KBucketItem<TKNodeId, TKPeerData>(nodeId, peerData));
                    }
                }
                else
                {
                    // item does not exist, but bucket does not have room, ping first entry
                    var n = l.First;

                    // start ping, check for async completion
                    var r = false;
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
                    if (r)
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
                            l.AddLast(new KBucketItem<TKNodeId, TKPeerData>(nodeId, peerData));
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
        LinkedListNode<KBucketItem<TKNodeId, TKPeerData>> GetNode(TKNodeId nodeId)
        {
            for (var i = l.First; i != null; i = i.Next)
                if (nodeId.Equals(i.Value.Id))
                    return i;

            return null;
        }

    }

}
