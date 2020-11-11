using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Alethic.Kademlia
{

    /// <summary>
    /// Provides methods to invoke endpoints with typed messages.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public class KInvoker<TNodeId> : IKInvoker<TNodeId>
        where TNodeId : unmanaged
    {

        readonly IKHost<TNodeId> host;
        readonly IKInvokerPolicy<TNodeId> policy;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="policy"></param>
        public KInvoker(IKHost<TNodeId> host, IKInvokerPolicy<TNodeId> policy)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
            this.policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TNodeId, KPingResponse<TNodeId>>> PingAsync(KProtocolEndpointSet<TNodeId> endpoints, CancellationToken cancellationToken = default)
        {
            return policy.InvokeAsync<KPingRequest<TNodeId>, KPingResponse<TNodeId>>(endpoints, new KPingRequest<TNodeId>(host.Endpoints.ToArray()));
        }

        public ValueTask<KResponse<TNodeId, KStoreResponse<TNodeId>>> StoreAsync(KProtocolEndpointSet<TNodeId> endpoints, in TNodeId key, KStoreRequestMode mode, in KValueInfo? value, CancellationToken cancellationToken = default)
        {
            return policy.InvokeAsync<KStoreRequest<TNodeId>, KStoreResponse<TNodeId>>(endpoints, new KStoreRequest<TNodeId>(key, mode, value));
        }

        public ValueTask<KResponse<TNodeId, KFindNodeResponse<TNodeId>>> FindNodeAsync(KProtocolEndpointSet<TNodeId> endpoints, in TNodeId key, CancellationToken cancellationToken = default)
        {
            return policy.InvokeAsync<KFindNodeRequest<TNodeId>, KFindNodeResponse<TNodeId>>(endpoints, new KFindNodeRequest<TNodeId>(key));
        }

        public ValueTask<KResponse<TNodeId, KFindValueResponse<TNodeId>>> FindValueAsync(KProtocolEndpointSet<TNodeId> endpoints, in TNodeId key, CancellationToken cancellationToken = default)
        {
            return policy.InvokeAsync<KFindValueRequest<TNodeId>, KFindValueResponse<TNodeId>>(endpoints, new KFindValueRequest<TNodeId>(key));
        }

    }

}
