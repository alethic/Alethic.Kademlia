namespace Cogito.Kademlia
{

    /// <summary>
    /// Base class of node protocol responses.
    /// </summary>
    public readonly struct KResponse<TNodeId, TKResponseBody>
        where TNodeId : unmanaged
        where TKResponseBody : struct, IKResponseBody<TNodeId>
    {

        readonly KResponseStatus status;
        readonly TNodeId sender;
        readonly TKResponseBody body;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="status"></param>
        /// <param name="body"></param>
        public KResponse(in TNodeId sender, KResponseStatus status, in TKResponseBody body)
        {
            this.sender = sender;
            this.status = status;
            this.body = body;
        }

        /// <summary>
        /// Gets the sender of this response.
        /// </summary>
        public TNodeId Sender => sender;

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
