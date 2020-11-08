using System;
using System.Collections.Generic;
using System.Linq;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a node and its associated endpoints.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public struct KNodeInfo<TNodeId>
        where TNodeId : unmanaged
    {

        readonly TNodeId id;
        readonly Uri[] endpoints;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="endpoints"></param>
        public KNodeInfo(in TNodeId id, IEnumerable<Uri> endpoints) :
            this(id, endpoints?.ToArray())
        {

        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="endpoints"></param>
        public KNodeInfo(in TNodeId id, Uri[] endpoints)
        {
            this.id = id;
            this.endpoints = endpoints;
        }

        /// <summary>
        /// Gets the node ID of the peer.
        /// </summary>
        public TNodeId Id => id;

        /// <summary>
        /// Gets the set of endpoints of the node.
        /// </summary>
        public Uri[] Endpoints => endpoints;

    }

}
