using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia.Network
{

    /// <summary>
    /// Manages correlations between outbound stateful calls with inbound response calls.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    class KIpCompletionQueue<TKNodeId, TResponse>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly TimeSpan timeout;
        readonly ConcurrentDictionary<(KIpEndpoint, TKNodeId, uint), TaskCompletionSource<TResponse>> queue;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public KIpCompletionQueue(TimeSpan timeout)
        {
            this.timeout = timeout;

            queue = new ConcurrentDictionary<(KIpEndpoint, TKNodeId, uint), TaskCompletionSource<TResponse>>();
        }

        /// <summary>
        /// Enqueues a wait for an inbound operation with the specified signature and returns a task to be resumed upon completion.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="remote"></param>
        /// <param name="magic"></param>
        /// <returns></returns>
        public Task<TResponse> Enqueue(in KIpEndpoint endpoint, in TKNodeId remote, uint magic, CancellationToken cancellationToken)
        {
            // generate a new task completion source hooked up with the given request information
            var tcs = queue.GetOrAdd((endpoint, remote, magic), k =>
            {
                var tcs = new TaskCompletionSource<TResponse>();
                var cts = CancellationTokenSource.CreateLinkedTokenSource(new CancellationTokenSource(timeout).Token, cancellationToken);
                cts.Token.Register(() => { queue.TryRemove(k, out _); tcs.TrySetCanceled(); }, useSynchronizationContext: false);
                return tcs;
            });

            // return task to user for waiting
            return tcs.Task;
        }

        /// <summary>
        /// Releases a wait for an inbound operation with the specified signature with the specified response.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="remote"></param>
        /// <param name="magic"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        public bool Release(in KIpEndpoint endpoint, in TKNodeId remote, uint magic, in TResponse response)
        {
            if (queue.TryRemove((endpoint, remote, magic), out var tcs))
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
        /// <param name="endpoint"></param>
        /// <param name="remote"></param>
        /// <param name="correlation"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        public bool Release(in KIpEndpoint endpoint, in TKNodeId remote, uint correlation, Exception exception)
        {
            if (queue.TryRemove((endpoint, remote, correlation), out var tcs))
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
