using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia.InMemory
{

    /// <summary>
    /// Provides a shared class that routes requests between in-memory protocol instances.
    /// </summary>
    public class KInMemoryProtocolBroker<TKNodeId> : IKInMemoryProtocolBroker<TKNodeId>
        where TKNodeId : unmanaged
    {

        readonly ConcurrentDictionary<TKNodeId, IKInMemoryProtocol<TKNodeId>> members;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public KInMemoryProtocolBroker()
        {
            members = new ConcurrentDictionary<TKNodeId, IKInMemoryProtocol<TKNodeId>>();
        }

        /// <summary>
        /// Registers a in-memory protocol with the broker.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="member"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask RegisterAsync(in TKNodeId node, IKInMemoryProtocol<TKNodeId> member, CancellationToken cancellationToken)
        {
            return RegisterAsyncInternal(node, member, cancellationToken);
        }

        async ValueTask RegisterAsyncInternal(TKNodeId node, IKInMemoryProtocol<TKNodeId> member, CancellationToken cancellationToken)
        {
            if (members.TryAdd(node, member))
                foreach (var m in members.Values)
                    await m.OnMemberRegisteredAsync(node);
        }

        /// <summary>
        /// Unregisters an in-memory protocol from the broker.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="member"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask UnregisterAsync(in TKNodeId node, IKInMemoryProtocol<TKNodeId> member, CancellationToken cancellationToken)
        {
            return UnregisterAsyncInternal(node, member, cancellationToken);
        }

        async ValueTask UnregisterAsyncInternal(TKNodeId node, IKInMemoryProtocol<TKNodeId> member, CancellationToken cancellationToken)
        {
            if (members.TryRemove(node, out _))
                foreach (var m in members.Values)
                    await m.OnMemberUnregisteredAsync(node);
        }

        delegate ValueTask<TResponseData> SendAsyncDelegate<TRequestData, TResponseData>(in TKNodeId sender, KInMemoryProtocolEndpoint<TKNodeId> target, in TRequestData request, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a request.
        /// </summary>
        /// <typeparam name="TResponseData"></typeparam>
        /// <typeparam name="TRequestData"></typeparam>
        /// <param name="sender"></param>
        /// <param name="getSendAsync"></param>
        /// <param name="target"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KResponse<TKNodeId, TResponseData>> SendAsync<TRequestData, TResponseData>(
            in TKNodeId sender,
            Func<IKInMemoryProtocol<TKNodeId>, SendAsyncDelegate<TRequestData, TResponseData>> getSendAsync,
            KInMemoryProtocolEndpoint<TKNodeId> target,
            in TRequestData request,
            CancellationToken cancellationToken)
            where TResponseData : struct, IKResponseData<TKNodeId>
        {
            if (members.TryGetValue(sender, out var protocol))
                if (protocol.Endpoints.OfType<KInMemoryProtocolEndpoint<TKNodeId>>().FirstOrDefault() is KInMemoryProtocolEndpoint<TKNodeId> source)
                    return SendAsyncInternal(sender, source, getSendAsync(protocol), target, request, cancellationToken);

            throw new KInMemoryProtocolException(KProtocolError.ProtocolNotAvailable, "Sender is not registered.");
        }

        async ValueTask<KResponse<TKNodeId, TResponseData>> SendAsyncInternal<TRequestData, TResponseData>(
            TKNodeId sender,
            KInMemoryProtocolEndpoint<TKNodeId> source,
            SendAsyncDelegate<TRequestData, TResponseData> sendAsync,
            KInMemoryProtocolEndpoint<TKNodeId> target,
            TRequestData request,
            CancellationToken cancellationToken)
            where TResponseData : struct, IKResponseData<TKNodeId>
        {
            if (await sendAsync(sender, source, request, cancellationToken) is TResponseData response)
                return new KResponse<TKNodeId, TResponseData>(target, target.Node, KResponseStatus.Success, response);

            throw new KInMemoryProtocolException(KProtocolError.EndpointNotAvailable, "Endpoint Not Available");
        }

        /// <summary>
        /// Sends a PING request.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="target"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TKNodeId, KPingResponse<TKNodeId>>> PingAsync(in TKNodeId sender, KInMemoryProtocolEndpoint<TKNodeId> target, in KPingRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            return SendAsync<KPingRequest<TKNodeId>, KPingResponse<TKNodeId>>(sender, protocol => protocol.PingRequestAsync, target, request, cancellationToken);
        }

        /// <summary>
        /// Sends a STORE request.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="target"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TKNodeId, KStoreResponse<TKNodeId>>> StoreAsync(in TKNodeId sender, KInMemoryProtocolEndpoint<TKNodeId> target, in KStoreRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            return SendAsync<KStoreRequest<TKNodeId>, KStoreResponse<TKNodeId>>(sender, protocol => protocol.StoreRequestAsync, target, request, cancellationToken);
        }

        /// <summary>
        /// Sends a FIND_NODE request.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="target"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>> FindNodeAsync(in TKNodeId sender, KInMemoryProtocolEndpoint<TKNodeId> target, in KFindNodeRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            return SendAsync<KFindNodeRequest<TKNodeId>, KFindNodeResponse<TKNodeId>>(sender, protocol => protocol.FindNodeRequestAsync, target, request, cancellationToken);
        }

        /// <summary>
        /// Sends a FIND_VALUE request.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="target"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TKNodeId, KFindValueResponse<TKNodeId>>> FindValueAsync(in TKNodeId sender, KInMemoryProtocolEndpoint<TKNodeId> target, in KFindValueRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            return SendAsync<KFindValueRequest<TKNodeId>, KFindValueResponse<TKNodeId>>(sender, protocol => protocol.FindValueRequestAsync, target, request, cancellationToken);
        }

    }

}
