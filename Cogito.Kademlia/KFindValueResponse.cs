using System;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a response to a FIND_VALUE request.
    /// </summary>
    public readonly struct KFindValueResponse<TNodeId> : IKResponseBody<TNodeId>
        where TNodeId : unmanaged
    {

        readonly KPeerInfo<TNodeId>[] peers;
        readonly KValueInfo? value;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="peers"></param>
        /// <param name="value"></param>
        public KFindValueResponse(KPeerInfo<TNodeId>[] peers, in KValueInfo? value)
        {
            this.peers = peers ?? throw new ArgumentNullException(nameof(peers));
            this.value = value;
        }

        /// <summary>
        /// Gets the set of peers and their endpoints returned by the lookup.
        /// </summary>
        public KPeerInfo<TNodeId>[] Peers => peers;

        /// <summary>
        /// Gets the value that was located.
        /// </summary>
        public KValueInfo? Value => value;

    }

}
