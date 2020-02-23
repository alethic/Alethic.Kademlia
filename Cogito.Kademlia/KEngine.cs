using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

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
        readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="network"></param>
        /// <param name="router"></param>
        /// <param name="logger"></param>
        public KEngine(IKRouter<TKNodeId, TKPeerData> router, ILogger logger = null)
        {
            this.router = router ?? throw new ArgumentNullException(nameof(router));
            this.logger = logger;

            // configure router
            this.router.Initialize(this);
            logger?.LogInformation("Attached to router as {NodeId}.", this.router.SelfId);
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
        /// Initiates a bootstrap connection to the specified endpoints.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async ValueTask ConnectAsync(IEnumerable<IKEndpoint<TKNodeId>> endpoints, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Bootstrapping network with connection to {Endpoints}.", endpoints);

            var r = await PingAsync(endpoints, cancellationToken);
            if (r.Status == KResponseStatus.Failure)
                throw new KProtocolException(KProtocolError.EndpointNotAvailable, "Unable to bootstrap off of the specified endpoints. No response.");

            await router.UpdatePeerAsync(r.Sender, null, r.Body.Endpoints, cancellationToken);
            await LookupAsync(SelfId, cancellationToken);
        }

        /// <summary>
        /// Initiates a bootstrap connection to the specified endpoint.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask ConnectAsync(IKEndpoint<TKNodeId> endpoint, CancellationToken cancellationToken = default)
        {
            return ConnectAsync(new[] { endpoint });
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
        /// Initiates a lookup for the specified key, returning the closest discovered node.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<TKNodeId> LookupAsync(in TKNodeId key, CancellationToken cancellationToken = default)
        {
            return LookupAsync(key, cancellationToken);
        }

        /// <summary>
        /// Begins a search process for the specified node.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<TKNodeId> LookupAsync(TKNodeId key, CancellationToken cancellationToken = default)
        {
            var wait = new HashSet<Task<(IKEndpoint<TKNodeId> Endpoint, IEnumerable<KPeerEndpointInfo<TKNodeId>> Peers)?>>();
            var comp = new KNodeIdDistanceComparer<TKNodeId>(key);

            // find our own closest peers to seed from
            var init = await router.GetPeersAsync(key, 3, cancellationToken);
            var hash = new HashSet<TKNodeId>(init.Select(i => i.Id));
            if (hash.Count == 0)
                throw new InvalidOperationException("Cannot conduct lookup, no initial nodes to contact.");

            // tracks the peers remaining to query
            var todo = new C5.IntervalHeap<TKNodeId>(router.K, comp);
            todo.AddAll(hash);

            // our nearest neighbor so far
            var near = todo.FindMin();

            // we continue until we have no more work todo
            while (todo.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // schedule queries of our closest nodes
                while (wait.Count < 3 && todo.Count > 0)
                {
                    // schedule new node to query
                    var n = todo.DeleteMin();
                    if (n.Equals(SelfId) == false)
                        wait.Add(FindNodeAsync(n, key, cancellationToken).AsTask());
                }

                // we have at least one task in the task pool to wait for
                if (wait.Count > 0)
                {
                    try
                    {
                        // wait for first finished task
                        var r = await Task.WhenAny(wait);
                        wait.Remove(r);

                        // skip failed tasks
                        if (r.Exception != null)
                            continue;

                        // iterate over newly retrieved peers
                        if (r.Result.HasValue)
                        {
                            foreach (var i in r.Result.Value.Peers)
                            {
                                // received node is closer than current
                                var n = i.Id;
                                if (n.Equals(SelfId) == false && comp.Compare(n, near) < 0)
                                {
                                    near = n;

                                    // update router and move peer to queried list
                                    await router.UpdatePeerAsync(n, null, i.Endpoints, cancellationToken);
                                    if (hash.Add(n))
                                    {
                                        todo.Add(n);

                                        // remove uninteresting nodes
                                        while (todo.Count > 20)
                                            todo.DeleteMax();
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        // ignore
                    }
                }
            }

            return near;
        }

        /// <summary>
        /// Issues a FIND_NODE request to the target, looking for the specified key, and returns the resolved peers and their endpoints.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<(IKEndpoint<TKNodeId>, IEnumerable<KPeerEndpointInfo<TKNodeId>>)?> FindNodeAsync(TKNodeId target, TKNodeId key, CancellationToken cancellationToken)
        {
            foreach (var endpoint in (await router.GetPeerAsync(target, cancellationToken)).Endpoints)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    // initial ping to node to collect real endpoints
                    var p = await endpoint.PingAsync(new KPingRequest<TKNodeId>(SelfData.Endpoints.ToArray()), cancellationToken);
                    if (p.Status == KResponseStatus.Success)
                    {
                        // update knowledge of peer
                        await router.UpdatePeerAsync(p.Sender, p.Endpoint, p.Body.Endpoints.ToArray(), cancellationToken);
                        cancellationToken.ThrowIfCancellationRequested();

                        // send find node to query for nodes to key
                        var l = await p.Endpoint.FindNodeAsync(new KFindNodeRequest<TKNodeId>(key), cancellationToken);
                        if (l.Status == KResponseStatus.Success)
                            return (l.Endpoint, l.Body.Peers);
                    }
                }
                catch (OperationCanceledException)
                {
                    // specific operation was canceled, move to next
                    continue;
                }
                catch (KProtocolException e) when (e.Error == KProtocolError.ProtocolNotAvailable)
                {
                    // the protocol is no longer available
                    break;
                }
                catch (KProtocolException e) when (e.Error == KProtocolError.EndpointNotAvailable)
                {
                    // the endpoint is no longer available
                    // TODO remove the endpoint from consideration; move to bottom of list?
                    continue;
                }
            }

            return null;
        }

        /// <summary>
        /// Attempts to execute the specified method against an endpoint.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KResponse<TKNodeId, TResponseBody>> TryAsync<TResponseBody>(IKEndpoint<TKNodeId> endpoint, Func<IKEndpoint<TKNodeId>, ValueTask<KResponse<TKNodeId, TResponseBody>>> func)
            where TResponseBody : struct, IKResponseData<TKNodeId>
        {
            if (endpoint.Protocol.Engine != this)
                throw new KProtocolException(KProtocolError.Invalid, "The endpoint specified originates from another Kademlia engine.");

            try
            {
                logger?.LogTrace("Attempting request against {Endpoint}.", endpoint);
                var r = await func(endpoint);
                if (r.Status == KResponseStatus.Success)
                    return r;
            }
            catch (OperationCanceledException)
            {
                // ignore
            }

            return default;
        }

        /// <summary>
        /// Attempts to execute the specified method against the provided endpoints.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KResponse<TKNodeId, TResponseBody>> TryAsync<TResponseBody>(IEnumerable<IKEndpoint<TKNodeId>> endpoints, Func<IKEndpoint<TKNodeId>, ValueTask<KResponse<TKNodeId, TResponseBody>>> func)
            where TResponseBody : struct, IKResponseData<TKNodeId>
        {
            foreach (var endpoint in endpoints)
            {
                var r = await TryAsync(endpoint, func);
                if (r.Status == KResponseStatus.Success)
                    return r;
            }

            return default;
        }

        /// <summary>
        /// Attempts to execute a PING request against each of the provided endpoints.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TKNodeId, KPingResponse<TKNodeId>>> PingAsync(IEnumerable<IKEndpoint<TKNodeId>> endpoints, CancellationToken cancellationToken = default)
        {
            return TryAsync(endpoints, ep => ep.PingAsync(new KPingRequest<TKNodeId>(SelfData.Endpoints.ToArray()), cancellationToken));
        }

        /// <summary>
        /// Attempts to execute a STORE request against each of the provided endpoints.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TKNodeId, KStoreResponse<TKNodeId>>> StoreAsync(IEnumerable<IKEndpoint<TKNodeId>> endpoints, TKNodeId key, ReadOnlyMemory<byte>? value, CancellationToken cancellationToken = default)
        {
            return TryAsync(endpoints, ep => ep.StoreAsync(new KStoreRequest<TKNodeId>(key, value), cancellationToken));
        }

        /// <summary>
        /// Attempts to execute a FIND_NODE request against each of the provided endpoints.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>> FindNodeAsync(IEnumerable<IKEndpoint<TKNodeId>> endpoints, TKNodeId key, CancellationToken cancellationToken = default)
        {
            return TryAsync(endpoints, ep => ep.FindNodeAsync(new KFindNodeRequest<TKNodeId>(key), cancellationToken));
        }

        /// <summary>
        /// Attempts to execute a FIND_VALUE request against each of the provided endpoints.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TKNodeId, KFindValueResponse<TKNodeId>>> FindValueAsync(IEnumerable<IKEndpoint<TKNodeId>> endpoints, TKNodeId key, CancellationToken cancellationToken = default)
        {
            return TryAsync(endpoints, ep => ep.FindValueAsync(new KFindValueRequest<TKNodeId>(key), cancellationToken));
        }

        /// <summary>
        /// Invoked to handle incoming PING requests.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KPingResponse<TKNodeId>> IKEngine<TKNodeId>.OnPingAsync(in TKNodeId sender, IKEndpoint<TKNodeId> endpoint, in KPingRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            logger?.LogDebug("Processing PING from {Sender}.", sender);
            return OnPingAsync(sender, endpoint, request, cancellationToken);
        }

        /// <summary>
        /// Invoked to handle incoming PING requests.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KPingResponse<TKNodeId>> OnPingAsync(TKNodeId sender, IKEndpoint<TKNodeId> endpoint, KPingRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            await router.UpdatePeerAsync(sender, endpoint, request.Endpoints, cancellationToken);
            return request.Respond(SelfData.Endpoints.ToArray());
        }

        /// <summary>
        /// Invoked to handle incoming STORE requests.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KStoreResponse<TKNodeId>> IKEngine<TKNodeId>.OnStoreAsync(in TKNodeId sender, IKEndpoint<TKNodeId> endpoint, in KStoreRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            logger?.LogDebug("Processing STORE from {Sender}.", sender);
            return OnStoreAsync(sender, endpoint, request, cancellationToken);
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
            await router.UpdatePeerAsync(source, endpoint, null, cancellationToken);
            return request.Respond();
        }

        /// <summary>
        /// Invoked to handle incoming FIND_NODE requests.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KFindNodeResponse<TKNodeId>> IKEngine<TKNodeId>.OnFindNodeAsync(in TKNodeId sender, IKEndpoint<TKNodeId> endpoint, in KFindNodeRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            logger?.LogDebug("Processing FIND_NODE from {Sender}.", sender);
            return OnFindNodeAsync(sender, endpoint, request, cancellationToken);
        }

        /// <summary>
        /// Invoked to handle incoming FIND_NODE requests.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KFindNodeResponse<TKNodeId>> OnFindNodeAsync(TKNodeId sender, IKEndpoint<TKNodeId> endpoint, KFindNodeRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            await router.UpdatePeerAsync(sender, endpoint, null, cancellationToken);
            return request.Respond(await router.GetPeersAsync(request.Key, 3, cancellationToken));
        }

        /// <summary>
        /// Invoked to handle incoming FIND_VALUE requests.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KFindValueResponse<TKNodeId>> IKEngine<TKNodeId>.OnFindValueAsync(in TKNodeId sender, IKEndpoint<TKNodeId> endpoint, in KFindValueRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            logger?.LogDebug("Processing FIND_VALUE from {Sender}.", sender);
            return OnFindValueAsync(sender, endpoint, request, cancellationToken);
        }

        /// <summary>
        /// Invoked to handle incoming FIND_VALUE requests.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KFindValueResponse<TKNodeId>> OnFindValueAsync(TKNodeId sender, IKEndpoint<TKNodeId> endpoint, KFindValueRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            await router.UpdatePeerAsync(sender, endpoint, null, cancellationToken);
            return request.Respond(null, await router.GetPeersAsync(request.Key, 3, cancellationToken)); // TODO respond with correct info
        }

    }

}
