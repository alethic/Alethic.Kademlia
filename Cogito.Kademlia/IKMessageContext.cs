using System;
using System.Collections.Generic;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Services available to a message encoder or decoder.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public interface IKMessageContext<TNodeId>
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Obtains a <see cref="IKProtocolEndpoint{TNodeId}"/> from a URI.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        IKProtocolEndpoint<TNodeId> ResolveEndpoint(Uri uri);

        /// <summary>
        /// Gets the allowable formats of the messages.
        /// </summary>
        IEnumerable<string> Formats { get; }

    }

}
