namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes an error with a Kademlia protocol.
    /// </summary>
    public enum KProtocolError
    {

        /// <summary>
        /// A generic error related to the protocol.
        /// </summary>
        Invalid = 0,

        /// <summary>
        /// The Kademlia protocol implementation is no longer available.
        /// </summary>
        ProtocolNotAvailable = 1,

        /// <summary>
        /// The specified attempted endpoint is no longer available.
        /// </summary>
        EndpointNotAvailable = 2,

    }

}
