namespace Alethic.Kademlia.Network
{

    /// <summary>
    /// Describes the format of an IP endpoint.
    /// </summary>
    public enum KIpAddressFamily : byte
    {

        /// <summary>
        /// Unspecified protocol.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Protocol is IPv4.
        /// </summary>
        IPv4 = 1,

        /// <summary>
        /// Protocol is IPv6.
        /// </summary>
        IPv6 = 2,

    }

}