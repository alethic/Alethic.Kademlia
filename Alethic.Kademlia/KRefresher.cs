using System;
using System.Threading;
using System.Threading.Tasks;

using Cogito.Threading;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Alethic.Kademlia
{

    /// <summary>
    /// Periodically refreshes the engine.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public class KRefresher<TNodeId> : IHostedService
        where TNodeId : unmanaged
    {

        readonly IKConnector<TNodeId> connector;
        readonly ILogger logger;

        readonly AsyncLock sync = new AsyncLock();

        CancellationTokenSource runCts;
        Task run;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="connector"></param>
        /// <param name="logger"></param>
        public KRefresher(IKConnector<TNodeId> connector, ILogger logger)
        {
            this.connector = connector ?? throw new ArgumentNullException(nameof(connector));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        /// <summary>
        /// Starts the processes of the refresher.
        /// </summary>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Starting periodic refresher.");

            using (await sync.LockAsync(cancellationToken))
            {
                if (run != null || runCts != null)
                    throw new InvalidOperationException();

                // begin new run processes
                runCts = new CancellationTokenSource();
                run = Task.WhenAll(Task.Run(() => PeriodicRefreshAsync(runCts.Token)));
            }
        }

        /// <summary>
        /// Stops the processes of the refresher.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Stopping periodic refresher.");

            using (await sync.LockAsync(cancellationToken))
            {
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
        /// Periodically refreshes the Kademlia tables until told to exit
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task PeriodicRefreshAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (cancellationToken.IsCancellationRequested == false)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    await connector.RefreshAsync(cancellationToken);
                    await Task.Delay(TimeSpan.FromMinutes(10), cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception e)
            {
                logger.LogError(e, "Exception occurred during periodic refresh.");
            }
        }

    }

}
