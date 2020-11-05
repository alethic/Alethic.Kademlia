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

        readonly IKEngine<TNodeId> engine;
        readonly IEnumerable<string> formats;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="sender"></param>
        /// <param name="target"></param>
        /// <param name="formats"></param>
        public KMessageContext(IKEngine<TNodeId> engine, IEnumerable<string> formats)
        {
            this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
            this.formats = formats ?? throw new ArgumentNullException(nameof(formats));
        }

        /// <summary>
        /// Resolves an endpoint for the given URI.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public IKProtocolEndpoint<TNodeId> ResolveEndpoint(Uri uri)
        {
            return engine.ResolveEndpoint(uri);
        }

        /// <summary>
        /// Gets the allowable formats.
        /// </summary>
        public IEnumerable<string> Formats => formats;

    }

}
