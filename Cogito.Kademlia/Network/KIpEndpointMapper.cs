using System.Collections.Generic;

namespace Cogito.Kademlia.Network
{

    /// <summary>
    /// Provides additional IP endpoints.
    /// </summary>
    public class KIpEndpointMapper
    {

        /// <summary>
        /// Gets any additional IP endpoints for the specified one.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public IEnumerable<KIpEndpoint> GetIpEndpoints(KIpEndpoint endpoint)
        {
            return null;
        }

    }

}
