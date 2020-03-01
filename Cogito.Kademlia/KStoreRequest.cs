namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a STORE request.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KStoreRequest<TKNodeId> : IKMessageBody<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Creates a response to the given request.
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public KStoreResponse<TKNodeId> Respond(KStoreResponseStatus status)
        {
            return new KStoreResponse<TKNodeId>(status);
        }

        readonly TKNodeId key;
        readonly KStoreRequestMode mode;
        readonly KValueInfo? value;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="mode"></param>
        /// <param name="value"></param>
        public KStoreRequest(in TKNodeId key, KStoreRequestMode mode, in KValueInfo? value)
        {
            this.key = key;
            this.mode = mode;
            this.value = value;
        }

        /// <summary>
        /// Specifies the key to be stored.
        /// </summary>
        public TKNodeId Key => key;

        /// <summary>
        /// Gets the mode of the store request.
        /// </summary>
        public KStoreRequestMode Mode => mode;

        /// <summary>
        /// Specifies the value to be stored with the key.
        /// </summary>
        public KValueInfo? Value => value;

    }

}
