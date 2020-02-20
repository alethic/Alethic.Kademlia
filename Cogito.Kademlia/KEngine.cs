using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
            var p = router.GetPeerAsync(r.Sender, cancellationToken);
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
        /// <param name="target"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask LookupAsync(in TKNodeId target, CancellationToken cancellationToken)
        {
            return LookupAsync(target, cancellationToken);
        }

        /// <summary>
        /// Begins a search process for the specified node.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask LookupAsync(TKNodeId key, CancellationToken cancellationToken)
        {
            var l = await router.GetPeersAsync(key, 3, cancellationToken);
            var t = l.ToArray().Select(i => FindNodeAsync(i.Id, key, cancellationToken).AsTask()).ToList();
            var f = new SortedSet<TKNodeId>(new KNodeIdDistanceComparer<TKNodeId>(key));

            while (true)
            {
                // wait for first finished task
                var r = await Task.WhenAny(t);
                t.Remove(r);
                var z = await r;

                //f.Add(z.ToArray().Select(i => i.NodeId)
            }
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
                    var l = await endpoint.FindNodeAsync(new KFindNodeRequest<TKNodeId>(key), cancellationToken);
                    if (l.Status == KResponseStatus.Success)
                        return l.Body.Peers;
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
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KPingResponse<TKNodeId>> IKEngine<TKNodeId>.OnPingAsync(in TKNodeId source, in KPingRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            return OnPingAsync(source, request, cancellationToken);
        }

        /// <summary>
        /// Invoked to handle incoming PING requests.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KPingResponse<TKNodeId>> OnPingAsync(TKNodeId source, KPingRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            await router.UpdatePeerAsync(source, request.Endpoints.ToArray(), cancellationToken);
            return new KPingResponse<TKNodeId>(SelfData.Endpoints.ToArray());
        }

        /// <summary>
        /// Invoked to handle incoming STORE requests.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KStoreResponse<TKNodeId>> IKEngine<TKNodeId>.OnStoreAsync(in TKNodeId source, in KStoreRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            return OnStoreAsync(source, request, cancellationToken);
        }

        /// <summary>
        /// Invoked to handle incoming STORE requests.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KStoreResponse<TKNodeId>> OnStoreAsync(TKNodeId source, KStoreRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            await router.UpdatePeerAsync(source, Enumerable.Empty<IKEndpoint<TKNodeId>>(), cancellationToken);
            return new KStoreResponse<TKNodeId>(request.Key);
        }

        /// <summary>
        /// Invoked to handle incoming FIND_NODE requests.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KFindNodeResponse<TKNodeId>> IKEngine<TKNodeId>.OnFindNodeAsync(in TKNodeId source, in KFindNodeRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            return OnFindNodeAsync(source, request, cancellationToken);
        }

        /// <summary>
        /// Invoked to handle incoming FIND_NODE requests.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KFindNodeResponse<TKNodeId>> OnFindNodeAsync(TKNodeId source, KFindNodeRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            await router.UpdatePeerAsync(source, Enumerable.Empty<IKEndpoint<TKNodeId>>(), cancellationToken);
            return new KFindNodeResponse<TKNodeId>(request.Key, await router.GetPeersAsync(request.Key, 3, cancellationToken));
        }

        /// <summary>
        /// Invoked to handle incoming FIND_VALUE requests.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KFindValueResponse<TKNodeId>> IKEngine<TKNodeId>.OnFindValueAsync(in TKNodeId source, in KFindValueRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            return OnFindValueAsync(source, request, cancellationToken);
        }

        /// <summary>
        /// Invoked to handle incoming FIND_VALUE requests.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KFindValueResponse<TKNodeId>> OnFindValueAsync(TKNodeId source, KFindValueRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            await router.UpdatePeerAsync(source, Enumerable.Empty<IKEndpoint<TKNodeId>>(), cancellationToken);
            return new KFindValueResponse<TKNodeId>(request.Key, null, new KPeerEndpointInfo<TKNodeId>[0]); // TODO respond with correct info
        }

    }

}
