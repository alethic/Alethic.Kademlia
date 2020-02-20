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
    /// <typeparam name="TResponseData"></typeparam>
    public class KIpResponseQueue<TKNodeId, TResponseData>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
        where TResponseData : struct, IKResponseData<TKNodeId>
    {

        readonly TimeSpan timeout;
        readonly ConcurrentDictionary<(KIpEndpoint, uint), TaskCompletionSource<KResponse<TKNodeId, TResponseData>>> queue;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public KIpResponseQueue(TimeSpan timeout)
        {
            this.timeout = timeout;

            queue = new ConcurrentDictionary<(KIpEndpoint, uint), TaskCompletionSource<KResponse<TKNodeId, TResponseData>>>();
        }

        /// <summary>
        /// Enqueues a wait for an inbound operation with the specified signature and returns a task to be resumed upon completion.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="magic"></param>
        /// <returns></returns>
        public Task<KResponse<TKNodeId, TResponseData>> WaitAsync(in KIpEndpoint endpoint, uint magic, CancellationToken cancellationToken)
        {
            // generate a new task completion source hooked up with the given request information
            var tcs = queue.GetOrAdd((endpoint, magic), k =>
           {
               var tcs = new TaskCompletionSource<KResponse<TKNodeId, TResponseData>>();
               var cts = CancellationTokenSource.CreateLinkedTokenSource(new CancellationTokenSource(timeout).Token, cancellationToken);
               cts.Token.Register(() => { queue.TryRemove(k, out _); tcs.TrySetCanceled(); }, useSynchronizationContext: false);
               return tcs;
           });

            // return task to user for waiting
            return tcs.Task;
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
            if (queue.TryRemove((endpoint, magic), out var tcs))
            {
                tcs.SetResult(data);
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
        /// <param name="correlation"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        public bool Respond(in KIpEndpoint endpoint, uint correlation, Exception exception)
        {
            if (queue.TryRemove((endpoint, correlation), out var tcs))
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
