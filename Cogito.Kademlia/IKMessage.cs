using System;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Represents any message with a body.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    /// <typeparam name="TBody"></typeparam>
    public interface IKMessage<TNodeId, TBody> : IKMessage<TNodeId>
        where TNodeId : unmanaged
        where TBody : struct, IKMessageBody<TNodeId>
    {

        /// <summary>
        /// Gets the body of the message.
        /// </summary>
        TBody? Body { get; }

    }

    /// <summary>
    /// Represents any message.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public interface IKMessage<TNodeId> : IEquatable<IKMessage<TNodeId>>
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Gets the header of the message.
        /// </summary>
        KMessageHeader<TNodeId> Header { get; }

    }

}

