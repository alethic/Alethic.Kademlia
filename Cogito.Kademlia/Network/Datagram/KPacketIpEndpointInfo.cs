using System.Runtime.CompilerServices;

namespace Cogito.Kademlia.Network.Datagram
{

    /// <summary>
    /// Provides a description of the layout.
    /// </summary>
    static class KPacketIpEndpointInfo
    {

        public static readonly int Size = Unsafe.SizeOf<KIpEndpoint>();

    }

}
