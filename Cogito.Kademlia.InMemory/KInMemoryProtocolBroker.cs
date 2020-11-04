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
    public class KInMemoryProtocolBroker<TNodeId> : IKInMemoryProtocolBroker<TNodeId>
        where TNodeId : unmanaged
    {

        readonly ConcurrentDictionary<TNodeId, IKInMemoryProtocol<TNodeId>> members;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public KInMemoryProtocolBroker()
        {
            members = new ConcurrentDictionary<TNodeId, IKInMemoryProtocol<TNodeId>>();
        }

        /// <summary>
        /// Registers a in-memory protocol with the broker.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="member"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask RegisterAsync(in TNodeId node, IKInMemoryProtocol<TNodeId> member, CancellationToken cancellationToken)
        {
            return RegisterAsyncInternal(node, member, cancellationToken);
        }

        async ValueTask RegisterAsyncInternal(TNodeId node, IKInMemoryProtocol<TNodeId> member, CancellationToken cancellationToken)
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
        public ValueTask UnregisterAsync(in TNodeId node, IKInMemoryProtocol<TNodeId> member, CancellationToken cancellationToken)
        {
            return UnregisterAsyncInternal(node, member, cancellationToken);
        }

        async ValueTask UnregisterAsyncInternal(TNodeId node, IKInMemoryProtocol<TNodeId> member, CancellationToken cancellationToken)
        {
            if (members.TryRemove(node, out _))
                foreach (var m in members.Values)
                    await m.OnMemberUnregisteredAsync(node);
        }

        delegate ValueTask<TResponseData> SendAsyncDelegate<TRequestData, TResponseData>(in TNodeId sender, KInMemoryProtocolEndpoint<TNodeId> target, in TRequestData request, CancellationToken cancellationToken);

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
        ValueTask<KResponse<TNodeId, TResponseData>> SendAsync<TRequestData, TResponseData>(
            in TNodeId sender,
            Func<IKInMemoryProtocol<TNodeId>, SendAsyncDelegate<TRequestData, TResponseData>> getSendAsync,
            KInMemoryProtocolEndpoint<TNodeId> target,
            in TRequestData request,
            CancellationToken cancellationToken)
            where TResponseData : struct, IKResponseBody<TNodeId>
        {
            if (members.TryGetValue(sender, out var protocol))
                if (protocol.Endpoints.OfType<KInMemoryProtocolEndpoint<TNodeId>>().FirstOrDefault() is KInMemoryProtocolEndpoint<TNodeId> source)
                    return SendAsyncInternal(sender, source, getSendAsync(protocol), target, request, cancellationToken);

            throw new KInMemoryProtocolException(KProtocolError.ProtocolNotAvailable, "Sender is not registered.");
        }

        async ValueTask<KResponse<TNodeId, TResponseData>> SendAsyncInternal<TRequestData, TResponseData>(
            TNodeId sender,
            KInMemoryProtocolEndpoint<TNodeId> source,
            SendAsyncDelegate<TRequestData, TResponseData> sendAsync,
            KInMemoryProtocolEndpoint<TNodeId> target,
            TRequestData request,
            CancellationToken cancellationToken)
            where TResponseData : struct, IKResponseBody<TNodeId>
        {
            if (await sendAsync(sender, source, request, cancellationToken) is TResponseData response)
                return new KResponse<TNodeId, TResponseData>(target.Node, KResponseStatus.Success, response);

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
        public ValueTask<KResponse<TNodeId, TResponse>> InvokeAsync<TRequest, TResponse>(in TNodeId sender, KInMemoryProtocolEndpoint<TNodeId> target, in TRequest request, CancellationToken cancellationToken)
            where TRequest : struct, IKRequestBody<TNodeId>
            where TResponse : struct, IKResponseBody<TNodeId>
        {
            return SendAsync<TRequest, TResponse>(sender, protocol => protocol.InvokeRequestAsync<TRequest, TResponse>, target, request, cancellationToken);
        }

    }

}
