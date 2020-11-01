using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Cogito.Kademlia.Net;

namespace Cogito.Kademlia.Tests.Json
{

    class TestIpProtocol<TKNodeId> : IKIpProtocol<TKNodeId>
        where TKNodeId : unmanaged
    {

        public IEnumerable<IKEndpoint<TKNodeId>> Endpoints => throw new System.NotImplementedException();

        public IKEndpoint<TKNodeId> CreateEndpoint(in KIpEndpoint endpoint)
        {
            return new KIpProtocolEndpoint<TKNodeId>(this, endpoint);
        }

        public ValueTask<KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>> FindNodeAsync(IKEndpoint<TKNodeId> target, in KFindNodeRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public ValueTask<KResponse<TKNodeId, KFindValueResponse<TKNodeId>>> FindValueAsync(IKEndpoint<TKNodeId> target, in KFindValueRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public ValueTask<KResponse<TKNodeId, KPingResponse<TKNodeId>>> PingAsync(IKEndpoint<TKNodeId> target, in KPingRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public ValueTask<KResponse<TKNodeId, KStoreResponse<TKNodeId>>> StoreAsync(IKEndpoint<TKNodeId> target, in KStoreRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

    }

}