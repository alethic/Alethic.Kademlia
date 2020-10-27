namespace Cogito.Kademlia.Protocols
{

    /// <summary>
    /// Defines the structure of a message.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public interface IKMessage<TKNodeId>
        where TKNodeId : unmanaged
    {

        /// <summary>
        /// Gets the header of the message.
        /// </summary>
        KMessageHeader<TKNodeId> Header { get; }

        /// <summary>
        /// Gets the body of the message.
        /// </summary>
        IKMessageBody<TKNodeId> Body { get; }

    }

}

