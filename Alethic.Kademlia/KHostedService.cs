using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

namespace Alethic.Kademlia
{

    /// <summary>
    /// Implements a <see cref="IHostedService"/> which starts and stops all of the given <see cref="IKService" /> instances.
    /// </summary>
    public class KHostedService : IHostedService
    {

        readonly IEnumerable<IKService> services;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="services"></param>
        public KHostedService(IEnumerable<IKService> services)
        {
            this.services = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <summary>
        /// Starts the services.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.WhenAll(services.Select(i => i.StartAsync(cancellationToken)));
        }

        /// <summary>
        /// Stops the services.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.WhenAll(services.Select(i => i.StopAsync(cancellationToken)));
        }

    }

}
