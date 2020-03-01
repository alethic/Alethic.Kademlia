namespace Cogito.Kademlia
{

    /// <summary>
    /// Descsribes the results of a store get operation.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KStoreGetResult<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly TKNodeId key;
        readonly KValueInfo? value;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public KStoreGetResult(in TKNodeId key, in KValueInfo? value)
        {
            this.key = key;
            this.value = value;
        }

        /// <summary>
        /// Gets the key that was retrieved.
        /// </summary>
        public TKNodeId Key => key;

        /// <summary>
        /// Gets the value retrieved from the store. If no value exists, <c>null</c> is returned.
        /// </summary>
        public KValueInfo? Value => value;

    }

}
