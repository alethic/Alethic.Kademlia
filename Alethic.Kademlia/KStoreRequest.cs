namespace Alethic.Kademlia
{

    /// <summary>
    /// Describes a STORE request.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public readonly struct KStoreRequest<TNodeId> : IKRequestBody<TNodeId>
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Creates a response to the given request.
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public KStoreResponse<TNodeId> Respond(KStoreResponseStatus status)
        {
            return new KStoreResponse<TNodeId>(status);
        }

        readonly TNodeId key;
        readonly KStoreRequestMode mode;
        readonly KValueInfo? value;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="mode"></param>
        /// <param name="value"></param>
        public KStoreRequest(in TNodeId key, KStoreRequestMode mode, in KValueInfo? value)
        {
            this.key = key;
            this.mode = mode;
            this.value = value;
        }

        /// <summary>
        /// Specifies the key to be stored.
        /// </summary>
        public TNodeId Key => key;

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
