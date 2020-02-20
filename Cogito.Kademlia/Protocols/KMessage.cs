namespace Cogito.Kademlia.Protocols
{

    /// <summary>
    /// Defines a message.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    /// <typeparam name="TBody"></typeparam>
    public readonly struct KMessage<TKNodeId, TBody> : IKMessage<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
        where TBody : struct, IKMessageBody<TKNodeId>
    {

        readonly KMessageHeader<TKNodeId> header;
        readonly TBody body;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="header"></param>
        /// <param name="body"></param>
        public KMessage(in KMessageHeader<TKNodeId> header, in TBody body)
        {
            this.header = header;
            this.body = body;
        }

        /// <summary>
        /// Gets the message header.
        /// </summary>
        public KMessageHeader<TKNodeId> Header => header;

        /// <summary>
        /// Gets the message body.
        /// </summary>
        public TBody Body => body;

        /// <summary>
        /// Gets the message body.
        /// </summary>
        IKMessageBody<TKNodeId> IKMessage<TKNodeId>.Body => body;

    }

}
