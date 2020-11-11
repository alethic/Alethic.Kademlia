//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;

//using Alethic.Kademlia.Network;

//namespace Alethic.Kademlia.Tests.Json
//{

//    class TestIpProtocol<TNodeId> : IKIpProtocol<TNodeId>
//        where TNodeId : unmanaged
//    {

//        public IEnumerable<IKProtocolEndpoint<TNodeId>> Endpoints => throw new System.NotImplementedException();

//        public IKProtocolEndpoint<TNodeId> CreateEndpoint(in KIpEndpoint endpoint)
//        {
//            return new KIpProtocolEndpoint<TNodeId>(this, endpoint);
//        }

//        public ValueTask<KResponse<TNodeId, KFindNodeResponse<TNodeId>>> FindNodeAsync(IKProtocolEndpoint<TNodeId> target, in KFindNodeRequest<TNodeId> request, CancellationToken cancellationToken)
//        {
//            throw new System.NotImplementedException();
//        }

//        public ValueTask<KResponse<TNodeId, KFindValueResponse<TNodeId>>> FindValueAsync(IKProtocolEndpoint<TNodeId> target, in KFindValueRequest<TNodeId> request, CancellationToken cancellationToken)
//        {
//            throw new System.NotImplementedException();
//        }

//        public ValueTask<KResponse<TNodeId, KPingResponse<TNodeId>>> PingAsync(IKProtocolEndpoint<TNodeId> target, in KPingRequest<TNodeId> request, CancellationToken cancellationToken)
//        {
//            throw new System.NotImplementedException();
//        }

//        public ValueTask<KResponse<TNodeId, KStoreResponse<TNodeId>>> StoreAsync(IKProtocolEndpoint<TNodeId> target, in KStoreRequest<TNodeId> request, CancellationToken cancellationToken)
//        {
//            throw new System.NotImplementedException();
//        }

//    }

//}