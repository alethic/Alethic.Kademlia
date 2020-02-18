namespace Cogito.Kademlia
{

    public static class KResponse
    {

        /// <summary>
        /// Creates a new <see cref="KResponse{TKNodeId, TKResponseBody}"/> instance.
        /// </summary>
        /// <typeparam name="TKNodeId"></typeparam>
        /// <typeparam name="TKResponseBody"></typeparam>
        /// <param name="sender"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public static KResponse<TKNodeId, TKResponseBody> Create<TKNodeId, TKResponseBody>(in TKNodeId sender, TKResponseBody body)
            where TKNodeId : IKNodeId<TKNodeId>
        {
            return new KResponse<TKNodeId, TKResponseBody>(sender, body);
        }

    }

    /// <summary>
    /// Base class of node protocol responses.
    /// </summary>
    public readonly struct KResponse<TKNodeId, TKResponseBody>
        where TKNodeId : IKNodeId<TKNodeId>
    {

        readonly TKNodeId sender;
        readonly TKResponseBody body;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="body"></param>
        public KResponse(TKNodeId sender, in TKResponseBody body)
        {
            this.sender = sender;
            this.body = body;
        }

        /// <summary>
        /// Gets the sender of this response.
        /// </summary>
        public TKNodeId Sender => sender;

        /// <summary>
        /// Gets the response body.
        /// </summary>
        public TKResponseBody Body => body;

    }

}
