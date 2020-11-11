using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Cogito.Linq;
using Cogito.Threading;

using Microsoft.Extensions.Logging;

namespace Alethic.Kademlia
{

    /// <summary>
    /// Represents a set of items within a <see cref="KFixedTableRouter{TNodeId}"/>.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    class KBucket<TNodeId> : IEnumerable<KBucketItem<TNodeId>>
        where TNodeId : unmanaged
    {

        readonly IKHost<TNodeId> host;
        readonly IKInvoker<TNodeId> invoker;
        readonly ILogger logger;
        readonly int k;

        readonly ReaderWriterLockSlim rw = new ReaderWriterLockSlim();
        readonly LinkedList<KBucketItem<TNodeId>> l = new LinkedList<KBucketItem<TNodeId>>();

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="invoker"></param>
        /// <param name="logger"></param>
        /// <param name="k"></param>
        public KBucket(IKHost<TNodeId> host, IKInvoker<TNodeId> invoker, ILogger logger, int k)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
            this.invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.k = k;
        }

        /// <summary>
        /// Updates the given node with the newly available endpoints.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="endpoints"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<IEnumerable<KNodeEndpointInfo<TNodeId>>> SelectAsync(in TNodeId nodeId, CancellationToken cancellationToken)
        {
            var n = GetNode(nodeId);
            if (n != null)
                return new ValueTask<IEnumerable<KNodeEndpointInfo<TNodeId>>>(new KNodeEndpointInfo<TNodeId>(n.Value.NodeId, n.Value.Endpoints).Yield());
            else
                return default;
        }

        /// <summary>
        /// Updates the given node with the newly available endpoints.
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="nodeId"></param>
        /// <param name="endpoints"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask UpdateAsync(in TNodeId nodeId, IEnumerable<IKProtocolEndpoint<TNodeId>> endpoints, CancellationToken cancellationToken)
        {
            return UpdateAsync(nodeId, endpoints, cancellationToken);
        }

        /// <summary>
        /// Updates the given node with the newly available endpoints.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="endpoints"></param>
        /// <param name="cancellationToken"></param>
        async ValueTask UpdateAsync(TNodeId nodeId, IEnumerable<IKProtocolEndpoint<TNodeId>> endpoints, CancellationToken cancellationToken)
        {
            var lk = rw.BeginUpgradableReadLock();

            try
            {
                var i = GetNode(nodeId);
                if (i != null)
                {
                    using (rw.BeginWriteLock())
                    {
                        // move to first if not already there
                        if (l.First != i)
                        {
                            logger.LogTrace("Peer {NodeId} exists, moving to head.", nodeId);
                            l.Remove(i);
                            l.AddFirst(i);
                        }

                        // incorporate new additional endpoints into end of set
                        if (endpoints != null)
                            foreach (var j in endpoints)
                                i.Value.Endpoints.Insert(j);
                    }
                }
                else if (l.Count < k)
                {
                    using (rw.BeginWriteLock())
                    {
                        logger.LogTrace("Peer {NodeId} does not exist, appending to tail.", nodeId);

                        // generate new peer entry
                        var p = new KBucketItem<TNodeId>(nodeId);

                        // incorporate new additional endpoints into end of set
                        if (endpoints != null)
                            foreach (var j in endpoints)
                                p.Endpoints.Insert(j);

                        // item does not exist, but bucket has room, insert at tail
                        l.AddFirst(p);
                    }
                }
                else
                {
                    logger.LogTrace("Peer {NodeId} not in bucket, however bucket is full. Beginning peer elimination.", nodeId);

                    // item does not exist, but bucket does not have room, ping last entry
                    var n = l.Last;

                    // start ping, check for async completion
                    var r = (KResponse<TNodeId, KPingResponse<TNodeId>>)default;
                    var t = invoker.PingAsync(n.Value.Endpoints, cancellationToken);

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
                        logger.LogTrace("PING to {ExistingNodeId} succeeded. Keeping existing peer and discarding {NodeId}.", n.Value.NodeId, nodeId);

                        // entry had response, move to tail, discard new entry
                        if (l.Count > 1)
                        {
                            using (rw.BeginWriteLock())
                            {
                                // will move to first if not already there
                                if (l.First != n)
                                {
                                    // remove if not already removed
                                    if (n.List != null)
                                        l.Remove(n);

                                    // node goes to head
                                    l.AddFirst(n.Value);
                                }
                            }
                        }
                    }
                    else
                    {
                        logger.LogTrace("PING failed to {PingNodeId}. Replacing with {NodeId}", n, nodeId);

                        using (rw.BeginWriteLock())
                        {
                            // first entry had no response, remove, insert new at tail
                            l.Remove(n);
                            var p = new KBucketItem<TNodeId>(nodeId);

                            // incorporate new additional endpoints into end of set
                            if (endpoints != null)
                                foreach (var j in endpoints)
                                    p.Endpoints.Insert(j);

                            l.AddFirst(p);
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
        LinkedListNode<KBucketItem<TNodeId>> GetNode(in TNodeId nodeId)
        {
            using (rw.BeginReadLock())
                for (var i = l.First; i != null; i = i.Next)
                    if (nodeId.Equals(i.Value.NodeId))
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
        public IEnumerator<KBucketItem<TNodeId>> GetEnumerator()
        {
            using (rw.BeginReadLock())
            {
                var a = new KBucketItem<TNodeId>[l.Count];
                l.CopyTo(a, 0);
                return ((IEnumerable<KBucketItem<TNodeId>>)a).GetEnumerator();
            }
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
