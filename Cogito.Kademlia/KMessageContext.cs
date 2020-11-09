using System;
using System.Collections.Generic;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Provides information about the message being sent.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public class KMessageContext<TNodeId> : IKMessageContext<TNodeId>
        where TNodeId : unmanaged
    {

        readonly IKHost<TNodeId> host;
        readonly IEnumerable<string> formats;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="sender"></param>
        /// <param name="target"></param>
        /// <param name="formats"></param>
        public KMessageContext(IKHost<TNodeId> host, IEnumerable<string> formats)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
            this.formats = formats ?? throw new ArgumentNullException(nameof(formats));
        }

        /// <summary>
        /// Resolves an endpoint for the given URI.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public IKProtocolEndpoint<TNodeId> ResolveEndpoint(Uri uri)
        {
            return host.ResolveEndpoint(uri);
        }

        /// <summary>
        /// Gets the allowable formats.
        /// </summary>
        public IEnumerable<string> Formats => formats;

    }

}
