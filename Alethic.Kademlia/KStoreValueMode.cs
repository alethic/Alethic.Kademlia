namespace Alethic.Kademlia
{

    /// <summary>
    /// Defines the type of item within a store.
    /// </summary>
    public enum KStoreValueMode
    {

        /// <summary>
        /// The store should record the value as a primary.
        /// </summary>
        Primary,

        /// <summary>
        /// The store should record the value as a replica.
        /// </summary>
        Replica,

    }

}
