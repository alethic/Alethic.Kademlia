using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Cogito.Kademlia.Network
{

    /// <summary>
    /// Manages correlations between outbound stateful calls with inbound response calls.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    /// <typeparam name="TResponseData"></typeparam>
    public class KIpResponseQueue<TKNodeId, TResponseData>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
        where TResponseData : struct, IKResponseData<TKNodeId>
    {

        /// <summary>
        /// Describes the endpoint and magic of an inbound packet to match.
        /// </summary>
        struct RoutingKey
        {

            /// <summary>
            /// Describes the inbound endpoint to match.
            /// </summary>
            public KIpEndpoint Endpoint;

            /// <summary>
            /// Describes the inbound magic to match.
            /// </summary>
            public uint Magic;

            /// <summary>
            /// Initializes a new instance.
            /// </summary>
            /// <param name="endpoint"></param>
            /// <param name="magic"></param>
            public RoutingKey(KIpEndpoint endpoint, uint magic)
            {
                Endpoint = endpoint;
                Magic = magic;
            }

            public override bool Equals(object obj)
            {
                return obj is RoutingKey q && Equals(q);
            }

            public bool Equals(RoutingKey other)
            {
                return Endpoint.Equals(other.Endpoint) && Magic == other.Magic;
            }

            public override int GetHashCode()
            {
                var h = new HashCode();
                h.Add(Endpoint);
                h.Add(Magic);
                return h.ToHashCode();
            }

        }

        readonly TimeSpan timeout;
        readonly ILogger logger;
        readonly ConcurrentDictionary<RoutingKey, TaskCompletionSource<KResponse<TKNodeId, TResponseData>>> queue;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="logger"></param>
        public KIpResponseQueue(TimeSpan timeout, ILogger logger = null)
        {
            this.timeout = timeout;
            this.logger = logger;

            queue = new ConcurrentDictionary<RoutingKey, TaskCompletionSource<KResponse<TKNodeId, TResponseData>>>();
        }

        /// <summary>
        /// Enqueues a wait for an inbound operation with the specified signature and returns a task to be resumed upon completion.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="magic"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<KResponse<TKNodeId, TResponseData>> WaitAsync(in KIpEndpoint endpoint, uint magic, CancellationToken cancellationToken)
        {
            // generate a new task completion source hooked up with the given request information
            var tcs = queue.GetOrAdd(new RoutingKey(endpoint, magic), k =>
            {
                var tcs = new TaskCompletionSource<KResponse<TKNodeId, TResponseData>>();
                var cts = CancellationTokenSource.CreateLinkedTokenSource(new CancellationTokenSource(timeout).Token, cancellationToken);
                cts.Token.Register(() => OnCancel(k, tcs), useSynchronizationContext: false);
                return tcs;
            });

            // return task to user for waiting
            return tcs.Task;
        }

        /// <summary>
        /// Invoked when a request is canceled.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="tcs"></param>
        void OnCancel(RoutingKey key, TaskCompletionSource<KResponse<TKNodeId, TResponseData>> tcs)
        {
            queue.TryRemove(key, out _);
            tcs.TrySetCanceled();
        }

        /// <summary>
        /// Releases a wait for an inbound operation with the specified signature with the specified data.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="magic"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool Respond(in KIpEndpoint endpoint, uint magic, in KResponse<TKNodeId, TResponseData> data)
        {
            if (queue.TryRemove(new RoutingKey(endpoint, magic), out var tcs))
            {
                logger?.LogTrace("Routing response to {Endpoint} {Magic}.", endpoint, magic);
                tcs.SetResult(data);
                return true;
            }
            else
            {
                logger?.LogTrace("No wait found for {Endpoint} {Magic}.", endpoint, magic);
                return false;
            }
        }

        /// <summary>
        /// Releases a wait for an inbound operation with the specified signature with an exception.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="magic"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        public bool Respond(in KIpEndpoint endpoint, uint magic, Exception exception)
        {
            if (queue.TryRemove(new RoutingKey(endpoint, magic), out var tcs))
            {
                logger?.LogTrace("Routing exception to {Endpoint} {Magic}.", endpoint, magic);
                tcs.SetException(exception);
                return true;
            }
            else
            {
                logger?.LogTrace("No wait found for {Endpoint} {Magic}.", endpoint, magic);
                return false;
            }
        }

    }

}
