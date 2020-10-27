namespace Cogito.Kademlia
{

    /// <summary>
    /// Base class of node protocol responses.
    /// </summary>
    public readonly struct KResponse<TKNodeId, TKResponseBody>
        where TKNodeId : unmanaged
        where TKResponseBody : struct, IKResponseData<TKNodeId>
    {

        readonly IKEndpoint<TKNodeId> endpoint;
        readonly KResponseStatus status;
        readonly TKNodeId sender;
        readonly TKResponseBody body;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="sender"></param>
        /// <param name="status"></param>
        /// <param name="body"></param>
        public KResponse(IKEndpoint<TKNodeId> endpoint, in TKNodeId sender, KResponseStatus status, in TKResponseBody body)
        {
            this.endpoint = endpoint;
            this.sender = sender;
            this.status = status;
            this.body = body;
        }

        /// <summary>
        /// Gets the endpoint which responded.
        /// </summary>
        public IKEndpoint<TKNodeId> Endpoint => endpoint;

        /// <summary>
        /// Gets the sender of this response.
        /// </summary>
        public TKNodeId Sender => sender;

        /// <summary>
        /// Gets the status of the request.
        /// </summary>
        public KResponseStatus Status => status;

        /// <summary>
        /// Gets the response body.
        /// </summary>
        public TKResponseBody Body => body;

    }

}
