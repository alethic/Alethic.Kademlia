using System;

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

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="sender"></param>
        /// <param name="target"></param>
        public KMessageContext(IKEngine<TNodeId> engine)
        {
            this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
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

    }

}
