namespace Cogito.Kademlia
{

    /// <summary>
    /// Options available to the <see cref="KEngine{TNodeId}"/>.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public class KEngineOptions<TNodeId>
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Gets or sets the ID of the Kademlia node.
        /// </summary>
        public TNodeId NodeId { get; set; }

    }

}
