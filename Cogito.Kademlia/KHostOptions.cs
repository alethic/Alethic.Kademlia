using System;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Options available to the <see cref="KHost{TNodeId}"/>.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public class KHostOptions<TNodeId>
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Gets or sets the ID of the Kademlia node.
        /// </summary>
        public TNodeId NodeId { get; set; }

        /// <summary>
        /// Additional static endpoint values to expose.
        /// </summary>
        public Uri[] Endpoints { get; set; }

    }

}
