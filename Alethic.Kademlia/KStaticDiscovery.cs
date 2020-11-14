using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Cogito.Threading;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Alethic.Kademlia
{

    /// <summary>
    /// Periodically connects to a static discovery point.
    /// </summary>
    public class KStaticDiscovery<TNodeId> : IKService
        where TNodeId : unmanaged
    {

        readonly IOptions<KStaticDiscoveryOptions> options;
        readonly IKHost<TNodeId> host;
        readonly IKConnector<TNodeId> connector;
        readonly ILogger logger;

        readonly AsyncLock sync = new AsyncLock();

        CancellationTokenSource runCts;
        Task run;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="host"></param>
        /// <param name="connector"></param>
        /// <param name="logger"></param>
        public KStaticDiscovery(IOptions<KStaticDiscoveryOptions> options, IKHost<TNodeId> host, IKConnector<TNodeId> connector, ILogger logger)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.host = host ?? throw new ArgumentNullException(nameof(host));
            this.connector = connector ?? throw new ArgumentNullException(nameof(connector));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using (await sync.LockAsync(cancellationToken))
            {
                if (run != null || runCts != null)
                    throw new InvalidOperationException();

                // begin new run processes
                runCts = new CancellationTokenSource();
                run = Task.WhenAll(Task.Run(() => DiscoveryRunAsync(runCts.Token)));

                // also connect when endpoints come and go
                host.EndpointsChanged += OnEndpointsChanged;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            using (await sync.LockAsync(cancellationToken))
            {
                host.EndpointsChanged -= OnEndpointsChanged;

                if (runCts != null)
                {
                    runCts.Cancel();
                    runCts = null;
                }

                if (run != null)
                {
                    try
                    {
                        await run;
                    }
                    catch (OperationCanceledException)
                    {
                        // ignore
                    }
                }
            }
        }

        /// <summary>
        /// Invoked when the host endpoints change.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void OnEndpointsChanged(object sender, EventArgs args)
        {
            Task.Run(() => DiscoveryAsync(CancellationToken.None));
        }

        /// <summary>
        /// Periodically publishes key/value pairs to the appropriate nodes.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task DiscoveryRunAsync(CancellationToken cancellationToken)
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                try
                {
                    // no reason to proceed without endpoints
                    if (host.Endpoints.Count == 0)
                        continue;

                    logger.LogInformation("Initiating periodic static discovery.");
                    await DiscoveryAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // ignore
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Unexpected exception occurred during static discovery.");
                }

                await Task.Delay(options.Value.Frequency, cancellationToken);
            }
        }

        /// <summary>
        /// Attempts to bootstrap the Kademlia host from the available multicast group members.
        /// </summary>
        /// <returns></returns>
        async ValueTask DiscoveryAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (options.Value.Endpoints != null)
                {
                    var ep = options.Value.Endpoints.Select(i => host.ResolveEndpoint(i)).Where(i => i != null).ToArray();
                    if (ep.Length > 0)
                        await connector.ConnectAsync(new KProtocolEndpointSet<TNodeId>(ep), cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (KProtocolException e) when (e.Error == KProtocolError.EndpointNotAvailable)
            {
                // ignore
            }
        }

    }

}

