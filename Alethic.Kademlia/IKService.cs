using System.Threading;
using System.Threading.Tasks;

namespace Alethic.Kademlia
{

    /// <summary>
    /// Describes a Kademlia service component that must be started and stopped.
    /// </summary>
    public interface IKService
    {

        /// <summary>
        /// Starts the service.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops the service.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task StopAsync(CancellationToken cancellationToken = default);

    }

}
