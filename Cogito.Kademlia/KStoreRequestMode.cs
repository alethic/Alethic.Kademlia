namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes the type of store being made.
    /// </summary>
    public enum KStoreRequestMode
    {

        /// <summary>
        /// The store request is an initial primary request.
        /// </summary>
        Primary = 0,

        /// <summary>
        /// The store request is a replica request.
        /// </summary>
        Replica = 1,

    }

}