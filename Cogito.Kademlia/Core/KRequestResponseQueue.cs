using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Cogito.Kademlia.Core
{

    /// <summary>
    /// Manages correlations between outbound stateful calls with inbound response calls.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public class KRequestResponseQueue<TNodeId, TKey>
        where TNodeId : unmanaged
    {

        readonly TimeSpan timeout;
        readonly ILogger logger;
        readonly ConcurrentDictionary<TKey, TaskCompletionSource<object>> queue;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="logger"></param>
        public KRequestResponseQueue(TimeSpan timeout, ILogger logger = null)
        {
            this.timeout = timeout;
            this.logger = logger;

            queue = new ConcurrentDictionary<TKey, TaskCompletionSource<object>>();
        }

        /// <summary>
        /// Enqueues a wait for an inbound operation with the specified signature and returns a task to be resumed upon completion.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<KResponse<TNodeId, TResponse>> WaitAsync<TResponse>(TKey key, CancellationToken cancellationToken)
            where TResponse : struct, IKResponseBody<TNodeId>
        {
            using (var cts = new CancellationTokenSource(timeout))
            {
                // generate a new task completion source hooked up with the given request information
                var tcs = queue.GetOrAdd(key, k =>
                {
                    var tcs = new TaskCompletionSource<object>();
                    var lnk = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
                    lnk.Token.Register(() => { queue.TryRemove(k, out _); tcs.TrySetCanceled(); });
                    return tcs;
                });

                try
                {
                    return (KResponse<TNodeId, TResponse>)await tcs.Task;
                }
                catch (OperationCanceledException) when (cts.IsCancellationRequested)
                {
                    throw new KProtocolException(KProtocolError.EndpointNotAvailable, "Timeout received.");
                }
            }
        }

        /// <summary>
        /// Releases a wait for an inbound operation with the specified signature with the specified data.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        public bool Respond<TResponse>(TKey key, in KResponse<TNodeId, TResponse> response)
            where TResponse : struct, IKResponseBody<TNodeId>
        {
            if (queue.TryRemove(key, out var tcs))
            {
                tcs.SetResult(response);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Releases a wait for an inbound operation with the specified signature with an exception.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        public bool Respond(TKey key, Exception exception)
        {
            if (queue.TryRemove(key, out var tcs))
            {
                tcs.SetException(exception);
                return true;
            }
            else
            {
                return false;
            }
        }

    }

}
