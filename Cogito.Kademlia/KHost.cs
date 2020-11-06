using System;
using System.Linq;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Provides an implementation of a Kademlia network engine. The <see cref="KHost{TNodeId}"/>
    /// class implements the core runtime logic of a Kademlia node.
    /// </summary>
    public class KHost<TNodeId> : IKHost<TNodeId>
        where TNodeId : unmanaged
    {

        readonly IOptions<KHostOptions<TNodeId>> options;
        readonly ILogger logger;

        readonly KEndpointSet<TNodeId> endpoints;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public KHost(IOptions<KHostOptions<TNodeId>> options, ILogger logger)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            endpoints = new KEndpointSet<TNodeId>();
        }

        /// <summary>
        /// Gets the Node ID of the node itself.
        /// </summary>
        public TNodeId SelfId => options.Value.NodeId;

        /// <summary>
        /// Gets the set of endpoints available on the engine.
        /// </summary>
        public KEndpointSet<TNodeId> Endpoints => endpoints;

        /// <summary>
        /// Resolves the protocol endpoint from the given URI.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public IKProtocolEndpoint<TNodeId> ResolveEndpoint(Uri uri)
        {
            return endpoints.Select(i => i.Protocol.ResolveEndpoint(uri)).FirstOrDefault(i => i != null);
        }

    }

}
