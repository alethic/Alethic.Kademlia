using System;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Defines the structure of a message.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public interface IKMessage<TNodeId> : IEquatable<IKMessage<TNodeId>>
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Gets the header of the message.
        /// </summary>
        KMessageHeader<TNodeId> Header { get; }

        /// <summary>
        /// Gets the body of the message.
        /// </summary>
        IKRequestBody<TNodeId> Body { get; }

    }

}

