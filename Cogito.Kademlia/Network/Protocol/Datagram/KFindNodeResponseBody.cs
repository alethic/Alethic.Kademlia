using System.Buffers;

namespace Cogito.Kademlia.Network.Protocol.Datagram
{

    /// <summary>
    /// Describes the data to send for a FIND_NODE response.
    /// </summary>
    public readonly ref struct KFindNodeResponseBody<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly TKNodeId nodeId;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="magic"></param>
        /// <param name="nodeId"></param>
        public KFindNodeResponseBody(in TKNodeId nodeId)
        {
            this.nodeId = nodeId;
        }

        /// <summary>
        /// Gets or sets the node ID to be located.
        /// </summary>
        public TKNodeId NodeId => nodeId;

    }

}
