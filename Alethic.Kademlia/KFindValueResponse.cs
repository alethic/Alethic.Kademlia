using System;

namespace Alethic.Kademlia
{

    /// <summary>
    /// Describes a response to a FIND_VALUE request.
    /// </summary>
    public readonly struct KFindValueResponse<TNodeId> : IKResponseBody<TNodeId>
        where TNodeId : unmanaged
    {

        readonly KNodeInfo<TNodeId>[] nodes;
        readonly KValueInfo? value;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="peers"></param>
        /// <param name="value"></param>
        public KFindValueResponse(KNodeInfo<TNodeId>[] peers, in KValueInfo? value)
        {
            this.nodes = peers ?? throw new ArgumentNullException(nameof(peers));
            this.value = value;
        }

        /// <summary>
        /// Gets the set of nodes and their endpoints returned by the lookup.
        /// </summary>
        public KNodeInfo<TNodeId>[] Nodes => nodes;

        /// <summary>
        /// Gets the value that was located.
        /// </summary>
        public KValueInfo? Value => value;

    }

}
