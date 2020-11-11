using System.Collections.Generic;

namespace Alethic.Kademlia
{

    /// <summary>
    /// Services available to a message encoder or decoder.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public interface IKMessageContext<TNodeId>
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Gets the allowable formats of the messages.
        /// </summary>
        IEnumerable<string> Formats { get; }

    }

}
