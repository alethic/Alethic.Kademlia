using System.Collections.Generic;

namespace Cogito.Kademlia.Network
{

    /// <summary>
    /// Describes an object which has UDP address information available.
    /// </summary>
    public interface IKIpEndpointProvider
    {

        /// <summary>
        /// Gets the IP endpoints.
        /// </summary>
        IList<KIpEndpoint> Endpoints { get; }

    }

}
