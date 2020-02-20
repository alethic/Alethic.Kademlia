using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Cogito.Kademlia.Core;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Provides an implementation of a Kademlia network engine. The <see cref="KEngine{TKNodeId, TKPeerData}"/>
    /// class implements the core runtime logic of a Kademlia node.
    /// </summary>
    public class KEngine<TKNodeId, TKPeerData> : IKEngine<TKNodeId, TKPeerData>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
        where TKPeerData : IKEndpointProvider<TKNodeId>
    {

        readonly IKRouter<TKNodeId, TKPeerData> router;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="router"></param>
        public KEngine(IKRouter<TKNodeId, TKPeerData> router)
        {
            this.router = router ?? throw new ArgumentNullException(nameof(router));
        }

        /// <summary>
        /// Gets the Node ID of the node itself.
        /// </summary>
        public TKNodeId SelfId => router.SelfId;

        /// <summary>
        /// Gets the peer data of the node itself.
        /// </summary>
        public TKPeerData SelfData => router.SelfData;

        /// <summary>
        /// Gets the router associated with the engine.
        /// </summary>
        public IKRouter<TKNodeId, TKPeerData> Router => router;

        /// <summary>
        /// Initiates a connection to the specified endpoint.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public async ValueTask ConnectAsync(IKEndpoint<TKNodeId> endpoint, CancellationToken cancellationToken = default)
        {
            if (endpoint.Protocol.Engine != this)
                throw new ArgumentException("Endpoint originates from different engine.");

            var r = await endpoint.PingAsync(new KPingRequest<TKNodeId>(SelfData.Endpoints.ToArray()), cancellationToken);
            await router.UpdatePeerAsync(r.Sender, r.Body.Endpoints.ToArray(), cancellationToken);
            await LookupAsync(SelfId, cancellationToken);
        }

        /// <summary>
        /// Gets the value for the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<ReadOnlyMemory<byte>> GetValueAsync(in TKNodeId key, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the value for the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask SetValueAsync(in TKNodeId key, ReadOnlySpan<byte> value, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Begins a search process for the specified node.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<TKNodeId> LookupAsync(TKNodeId key, CancellationToken cancellationToken)
        {
            // find our own closest peers and populate a priority queue to work through
            var l = await router.GetPeersAsync(key, 3, cancellationToken);
            var q = new FibonacciQueue<TKNodeId, TKNodeId>(l.Select(i => i.Id), i => i, new KNodeIdDistanceComparer<TKNodeId>(key).Compare);
            var d = new FibonacciQueue<TKNodeId, TKNodeId>(Enumerable.Empty<TKNodeId>(), i => i, new KNodeIdDistanceComparer<TKNodeId>(key).Compare);
            var t = new List<Task<IEnumerable<KPeerEndpointInfo<TKNodeId>>>>(4);

            while (q.Count > 0)
            {
                // schedule queries of our closest nodes
                while (t.Count < 4 && q.Count > 0)
                {
                    // schedule query from node, and move to final queue
                    var n = q.Dequeue();
                    if (n.Equals(SelfId) == false)
                    {
                        t.Add(FindNodeAsync(n, key, cancellationToken).AsTask());
                        d.Enqueue(n);
                    }
                }

                // we have at least one task in the task pool to wait for
                if (t.Count > 0)
                {
                    // wait for first finished task
                    var r = await Task.WhenAny(t);
                    t.Remove(r);
                    var z = await r;

                    // iterate over newly retrieved peers
                    foreach (var i in z)
                    {
                        // ignore self references
                        if (i.Id.Equals(SelfId) == false)
                        {
                            // update router and move peer to queried list
                            await router.UpdatePeerAsync(i.Id, i.Endpoints, cancellationToken);
                            q.Enqueue(i.Id);
                        }
                    }
                }
            }

            // return closest completed node
            return d.Dequeue();
        }

        /// <summary>
        /// Issues a FIND_NODE request to the target, looking for the specified key, and returns the resolved peers and their endpoints.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<IEnumerable<KPeerEndpointInfo<TKNodeId>>> FindNodeAsync(TKNodeId target, TKNodeId key, CancellationToken cancellationToken)
        {
            foreach (var endpoint in (await router.GetPeerAsync(target, cancellationToken)).Endpoints)
            {
                try
                {
                    // initial ping to node to collect real endpoints
                    var p = await endpoint.PingAsync(new KPingRequest<TKNodeId>(SelfData.Endpoints.ToArray()), cancellationToken);
                    if (p.Status == KResponseStatus.Success)
                    {
                        // update knowledge of peer
                        await router.UpdatePeerAsync(p.Sender, p.Body.Endpoints.ToArray(), cancellationToken);

                        // send find node to query for nodes to key
                        var l = await endpoint.FindNodeAsync(new KFindNodeRequest<TKNodeId>(key), cancellationToken);
                        if (l.Status == KResponseStatus.Success)
                            return l.Body.Peers;
                    }
                }
                catch (OperationCanceledException)
                {
                    // ignore
                }
            }

            return Enumerable.Empty<KPeerEndpointInfo<TKNodeId>>();
        }

        /// <summary>
        /// Invoked to handle incoming PING requests.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KPingResponse<TKNodeId>> IKEngine<TKNodeId>.OnPingAsync(in TKNodeId source, IKEndpoint<TKNodeId> endpoint, in KPingRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            return OnPingAsync(source, endpoint, request, cancellationToken);
        }

        /// <summary>
        /// Invoked to handle incoming PING requests.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KPingResponse<TKNodeId>> OnPingAsync(TKNodeId source, IKEndpoint<TKNodeId> endpoint, KPingRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            await router.UpdatePeerAsync(source, request.Endpoints, cancellationToken);
            await router.UpdatePeerAsync(source, new IKEndpoint<TKNodeId>[] { endpoint }, cancellationToken);
            return request.Respond(SelfData.Endpoints.ToArray());
        }

        /// <summary>
        /// Invoked to handle incoming STORE requests.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KStoreResponse<TKNodeId>> IKEngine<TKNodeId>.OnStoreAsync(in TKNodeId source, IKEndpoint<TKNodeId> endpoint, in KStoreRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            return OnStoreAsync(source, endpoint, request, cancellationToken);
        }

        /// <summary>
        /// Invoked to handle incoming STORE requests.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KStoreResponse<TKNodeId>> OnStoreAsync(TKNodeId source, IKEndpoint<TKNodeId> endpoint, KStoreRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            await router.UpdatePeerAsync(source, new IKEndpoint<TKNodeId>[] { endpoint }, cancellationToken);
            return request.Respond();
        }

        /// <summary>
        /// Invoked to handle incoming FIND_NODE requests.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KFindNodeResponse<TKNodeId>> IKEngine<TKNodeId>.OnFindNodeAsync(in TKNodeId source, IKEndpoint<TKNodeId> endpoint, in KFindNodeRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            return OnFindNodeAsync(source, endpoint, request, cancellationToken);
        }

        /// <summary>
        /// Invoked to handle incoming FIND_NODE requests.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KFindNodeResponse<TKNodeId>> OnFindNodeAsync(TKNodeId source, IKEndpoint<TKNodeId> endpoint, KFindNodeRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            await router.UpdatePeerAsync(source, new IKEndpoint<TKNodeId>[] { endpoint }, cancellationToken);
            return request.Respond(await router.GetPeersAsync(request.Key, 3, cancellationToken));
        }

        /// <summary>
        /// Invoked to handle incoming FIND_VALUE requests.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KFindValueResponse<TKNodeId>> IKEngine<TKNodeId>.OnFindValueAsync(in TKNodeId source, IKEndpoint<TKNodeId> endpoint, in KFindValueRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            return OnFindValueAsync(source, endpoint, request, cancellationToken);
        }

        /// <summary>
        /// Invoked to handle incoming FIND_VALUE requests.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KFindValueResponse<TKNodeId>> OnFindValueAsync(TKNodeId source, IKEndpoint<TKNodeId> endpoint, KFindValueRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            await router.UpdatePeerAsync(source, new IKEndpoint<TKNodeId>[] { endpoint }, cancellationToken);
            return request.Respond(null, new KPeerEndpointInfo<TKNodeId>[0]); // TODO respond with correct info
        }

    }

}
