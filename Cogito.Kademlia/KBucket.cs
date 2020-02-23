using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Cogito.Threading;

using Microsoft.Extensions.Logging;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Represents a set of items within a <see cref="KFixedTableRouter{TKNodeId}"/>.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    /// <typeparam name="TKPeerData"></typeparam>
    class KBucket<TKNodeId, TKPeerData> : IEnumerable<KBucketItem<TKNodeId, TKPeerData>>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
        where TKPeerData : IKEndpointProvider<TKNodeId>, new()
    {

        readonly IKRouter<TKNodeId> router;
        readonly int k;
        readonly ILogger logger;
        readonly ReaderWriterLockSlim rw = new ReaderWriterLockSlim();
        readonly LinkedList<KBucketItem<TKNodeId, TKPeerData>> l = new LinkedList<KBucketItem<TKNodeId, TKPeerData>>();

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="router"></param>
        /// <param name="k"></param>
        /// <param name="logger"></param>
        public KBucket(IKRouter<TKNodeId> router, int k, ILogger logger = null)
        {
            this.router = router ?? throw new ArgumentNullException(nameof(router));
            this.k = k;
            this.logger = logger;
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
        /// <param name="additional"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal ValueTask UpdatePeerAsync(in TKNodeId nodeId, IKEndpoint<TKNodeId> endpoint, IEnumerable<IKEndpoint<TKNodeId>> additional, CancellationToken cancellationToken)
        {
            return UpdatePeerAsync(nodeId, endpoint, additional, cancellationToken);
        }

        /// <summary>
        /// Updates the given node with the newly available endpoints.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="additional"></param>
        /// <param name="cancellationToken"></param>
        async ValueTask UpdatePeerAsync(TKNodeId nodeId, IKEndpoint<TKNodeId> endpoint, IEnumerable<IKEndpoint<TKNodeId>> additional, CancellationToken cancellationToken)
        {
            logger?.LogTrace("Updating peer {NodeId} with {Endpoint}.", nodeId, endpoint);

            var lk = rw.BeginUpgradableReadLock();

            try
            {
                var i = GetNode(nodeId);
                if (i != null)
                {
                    using (rw.BeginWriteLock())
                    {
                        logger?.LogTrace("Peer {NodeId} exists, moving to tail.", nodeId);
                        l.Remove(i);
                        l.AddLast(i);

                        // promote contact endpoint to top
                        if (endpoint != null)
                        {
                            logger?.LogTrace("Promoting {Endpoint} for peer {NodeId}.", endpoint, nodeId);
                            i.Value.Data.Endpoints.AddFirst(endpoint);
                        }

                        // incorporate new additional endpoints into end of set
                        if (additional != null)
                            foreach (var j in additional)
                                if (i.Value.Data.Endpoints.Contains(j) == false)
                                    i.Value.Data.Endpoints.AddLast(j);
                    }
                }
                else if (l.Count < k)
                {
                    using (rw.BeginWriteLock())
                    {
                        logger?.LogTrace("Peer {NodeId} does not exist, appending to tail.", nodeId);

                        // generate new peer entry
                        var p = new TKPeerData();

                        // promote contact endpoint to top
                        if (endpoint != null)
                        {
                            logger?.LogDebug("Promoting {Endpoint} for peer {NodeId}.", endpoint, nodeId);
                            p.Endpoints.AddFirst(endpoint);
                        }

                        // incorporate new additional endpoints into end of set
                        if (additional != null)
                            foreach (var j in additional)
                                if (p.Endpoints.Contains(j) == false)
                                    p.Endpoints.AddLast(j);

                        // item does not exist, but bucket has room, insert at tail
                        l.AddLast(new KBucketItem<TKNodeId, TKPeerData>(nodeId, p));
                    }
                }
                else
                {
                    logger?.LogTrace("Peer {NodeId} not in bucket, however bucket is full. Beginning peer elimination.", nodeId);

                    // item does not exist, but bucket does not have room, ping first entry
                    var n = l.First;

                    // start ping, check for async completion
                    var r = (KResponse<TKNodeId, KPingResponse<TKNodeId>>)default;
                    var t = endpoint.Protocol.Engine.PingAsync(n.Value.Data.Endpoints, cancellationToken);

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
                    if (r.Status == KResponseStatus.Success)
                    {
                        logger?.LogTrace("PING to {ExistingNodeId} succeeded. Keeping existing peer and discarding {NodeId}.", n.Value.Id, nodeId);

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
                        logger?.LogTrace("PING failed to {PingNodeId}. Replacing with {NodeId}", n, nodeId);

                        using (rw.BeginWriteLock())
                        {
                            // first entry had no response, remove, insert new at tail
                            l.Remove(n);
                            var p = new TKPeerData();

                            // incorporate new additional endpoints into end of set
                            if (additional != null)
                                foreach (var j in additional)
                                    if (p.Endpoints.Contains(j) == false)
                                        p.Endpoints.AddLast(j);

                            // promote contact endpoint to top
                            if (endpoint != null)
                                p.Endpoints.AddFirst(endpoint);

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
