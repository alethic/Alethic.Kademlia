using System.Collections.Generic;
using System.Linq;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a FIND_NODE request.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KFindNodeRequest<TKNodeId> : IKMessageBody<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Creates a response to the given request.
        /// </summary>
        /// <param name="peers"></param>
        /// <returns></returns>
        public KFindNodeResponse<TKNodeId> Respond(KPeerEndpointInfo<TKNodeId>[] peers)
        {
            return new KFindNodeResponse<TKNodeId>(peers);
        }

        /// <summary>
        /// Creates a response to the given request.
        /// </summary>
        /// <param name="peers"></param>
        /// <returns></returns>
        public KFindNodeResponse<TKNodeId> Respond(IEnumerable<KPeerEndpointInfo<TKNodeId>> peers)
        {
            return new KFindNodeResponse<TKNodeId>(peers.ToArray());
        }

        readonly TKNodeId key;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        public KFindNodeRequest(in TKNodeId key)
        {
            this.key = key;
        }

        /// <summary>
        /// Specifies the node ID to be located.
        /// </summary>
        public TKNodeId Key => key;

    }

}
