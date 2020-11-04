using System;

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

    }

}
