namespace Cogito.Kademlia
{

    /// <summary>
    /// Base class of node protocol responses.
    /// </summary>
    public readonly struct KResponse<TKNodeId, TKResponseBody>
        where TKNodeId : IKNodeId<TKNodeId>
    {

        readonly TKNodeId sender;
        readonly TKNodeId target;
        readonly TKResponseBody body;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="target"></param>
        /// <param name="body"></param>
        public KResponse(TKNodeId sender, TKNodeId target, in TKResponseBody body)
        {
            this.sender = sender;
            this.target = target;
            this.body = body;
        }

        /// <summary>
        /// Gets the sender of this response.
        /// </summary>
        public TKNodeId Sender => sender;

        /// <summary>
        /// Gets the target of this response.
        /// </summary>
        public TKNodeId Target => target;

        /// <summary>
        /// Gets the response body.
        /// </summary>
        public TKResponseBody Body => body;

    }

}
