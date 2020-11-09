using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Cogito.Linq;

using Microsoft.Extensions.Logging;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Provides services to initiate and refresh connections.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public class KConnector<TNodeId> : IKConnector<TNodeId>
        where TNodeId : unmanaged
    {

        readonly IKHost<TNodeId> host;
        readonly IKRouter<TNodeId> router;
        readonly IKInvoker<TNodeId> invoker;
        readonly IKLookup<TNodeId> lookup;
        readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="router"></param>
        /// <param name="invoker"></param>
        /// <param name="lookup"></param>
        /// <param name="logger"></param>
        public KConnector(IKHost<TNodeId> host, IKRouter<TNodeId> router, IKInvoker<TNodeId> invoker, IKLookup<TNodeId> lookup, ILogger logger)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
            this.router = router ?? throw new ArgumentNullException(nameof(router));
            this.invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
            this.lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initiates a bootstrap connection to the specified endpoints.
        /// </summary>
        /// <param name="targets"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async ValueTask ConnectAsync(KProtocolEndpointSet<TNodeId> targets, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Bootstrapping network with connection to {Endpoints}.", targets);

            // ping node, which ensures availability and populates tables upon response
            var r = await invoker.PingAsync(targets, cancellationToken);
            if (r.Status == KResponseStatus.Failure)
                throw new KProtocolException(KProtocolError.EndpointNotAvailable, "Unable to bootstrap off of the specified endpoints. No response.");

            await router.UpdateAsync(r.Header.Sender, targets, cancellationToken);
            await lookup.LookupNodeAsync(host.SelfId, cancellationToken);
        }

        /// <summary>
        /// Initiates a bootstrap connection to the specified endpoint.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask ConnectAsync(IKProtocolEndpoint<TNodeId> endpoint, CancellationToken cancellationToken = default)
        {
            return ConnectAsync(new KProtocolEndpointSet<TNodeId>(endpoint.Yield()));
        }

        /// <summary>
        /// Initiates a refresh of the Kademlia network by initiating lookups for random node IDs different distances from
        /// the current node ID.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask RefreshAsync(CancellationToken cancellationToken = default)
        {
            return new ValueTask(Task.WhenAll(Enumerable.Range(1, KNodeId<TNodeId>.SizeOf * 8 - 1).Select(i => KNodeId<TNodeId>.Randomize(host.SelfId, i)).Select(i => lookup.LookupNodeAsync(i, cancellationToken).AsTask())));
        }

    }

}
