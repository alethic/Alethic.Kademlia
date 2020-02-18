using System;
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
        /// <param name="self"></param>
        /// <param name="protocol"></param>
        /// <param name="router"></param>
        public KEngine(IKRouter<TKNodeId, TKPeerData> router)
        {
            this.router = router;
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

    }

}
