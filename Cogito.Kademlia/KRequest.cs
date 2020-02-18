namespace Cogito.Kademlia
{

    /// <summary>
    /// Base class of node protocol responses.
    /// </summary>
    public readonly struct KRequest<TKNodeId, TKRequestBody>
        where TKNodeId : IKNodeId<TKNodeId>
    {

        readonly TKNodeId sender;
        readonly TKRequestBody body;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="body"></param>
        public KRequest(in TKNodeId sender, TKRequestBody body)
        {
            this.sender = sender;
            this.body = body;
        }

        /// <summary>
        /// Gets the sender of this request.
        /// </summary>
        public TKNodeId Sender => sender;

        /// <summary>
        /// Gets the request body.
        /// </summary>
        public TKRequestBody Body => body;

    }

}
