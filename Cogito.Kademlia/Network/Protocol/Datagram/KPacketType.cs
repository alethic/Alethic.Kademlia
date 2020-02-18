namespace Cogito.Kademlia.Network.Protocol.Datagram
{

    /// <summary>
    /// Describes a type of Kademlia request.
    /// </summary>
    public enum KPacketType : sbyte
    {

        /// <summary>
        /// Packet is a Kademlia PING request.
        /// </summary>
        PingRequest = 1,

        /// <summary>
        /// Packets is a Kademlia PING response.
        /// </summary>
        PingResponse = -1,

        /// <summary>
        /// Packet is a Kademlia STORE request.
        /// </summary>
        StoreRequest = 2,

        /// <summary>
        /// Packet is a Kademlia STORE response.
        /// </summary>
        StoreResponse = -2,

        /// <summary>
        /// Packet is a Kademlia FIND_NODE request.
        /// </summary>
        FindNodeRequest = 3,

        /// <summary>
        /// Packet is a Kademlia FIND_NODE response.
        /// </summary>
        FindNodeResponse = -3,

        /// <summary>
        /// Packet is a Kademlia FIND_VALUE request.
        /// </summary>
        FindValueRequest = 4,

        /// <summary>
        /// Packet is a Kademlia FIND_VALUE response.
        /// </summary>
        FindValueResponse = -4,

    }

}
