using System;

namespace Cogito.Kademlia.Network.Protocol.Datagram
{

    /// <summary>
    /// Describes the data to send for a FIND_NODE response.
    /// </summary>
    public readonly ref struct KFindNodeResponseBody<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly TKNodeId key;
        readonly ReadOnlyMemory<KIpPeer<TKNodeId>> peers;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="peers"></param>
        public KFindNodeResponseBody(in TKNodeId key, ReadOnlyMemory<KIpPeer<TKNodeId>> peers)
        {
            this.key = key;
            this.peers = peers;
        }

        /// <summary>
        /// Gets the node ID that was searched for.
        /// </summary>
        public TKNodeId Key => key;

        /// <summary>
        /// Gets the peers that were returned from the search.
        /// </summary>
        public ReadOnlyMemory<KIpPeer<TKNodeId>> Endpoints => peers;

    }

}
