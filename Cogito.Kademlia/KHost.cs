﻿using System;
using System.Collections.Generic;
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
        readonly HashSet<Uri> endpoints = new HashSet<Uri>();
        readonly HashSet<IKProtocol<TNodeId>> protocols = new HashSet<IKProtocol<TNodeId>>();

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public KHost(IOptions<KHostOptions<TNodeId>> options, ILogger logger)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the Node ID of the node itself.
        /// </summary>
        public TNodeId SelfId => options.Value.NodeId;

        /// <summary>
        /// Gets the set of endpoints available on the engine.
        /// </summary>
        public IReadOnlyCollection<Uri> Endpoints => endpoints;

        /// <summary>
        /// Registers an endpoint.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public bool RegisterEndpoint(Uri uri)
        {
            if (endpoints.Add(uri))
            {
                OnEndpointsChanged();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Unregisters an endpoint.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public bool UnregisterEndpoint(Uri uri)
        {
            if (endpoints.Remove(uri))
            {
                OnEndpointsChanged();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Raised when the set of endpoints has changed.
        /// </summary>
        public event EventHandler EndpointsChanged;

        /// <summary>
        /// Raises the <see cref="EndpointsChanged"/> event.
        /// </summary>
        void OnEndpointsChanged()
        {
            EndpointsChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Resolves the protocol endpoint from the given URI.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public IKProtocolEndpoint<TNodeId> ResolveEndpoint(Uri uri)
        {
            return protocols.Select(i => i.ResolveEndpoint(uri)).FirstOrDefault(i => i != null);
        }

        /// <summary>
        /// Gets the set of protocols available on the engine.
        /// </summary>
        public IReadOnlyCollection<IKProtocol<TNodeId>> Protocols => protocols;

        /// <summary>
        /// Registers a protocol.
        /// </summary>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public bool RegisterProtocol(IKProtocol<TNodeId> protocol)
        {
            if (protocols.Add(protocol))
            {
                OnProtocolsChanged();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Unregisters a protocol.
        /// </summary>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public bool UnregisterProtocol(IKProtocol<TNodeId> protocol)
        {
            if (protocols.Remove(protocol))
            {
                OnProtocolsChanged();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Raised when the set of protocols has changed.
        /// </summary>
        public event EventHandler ProtocolsChanged;

        /// <summary>
        /// Raises the <see cref="ProtocolsChanged"/> event.
        /// </summary>
        void OnProtocolsChanged()
        {
            ProtocolsChanged?.Invoke(this, EventArgs.Empty);
        }

    }

}
