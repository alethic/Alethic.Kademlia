using System.Collections.Generic;
using System.Linq;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a FIND_VALUE request.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KFindValueRequest<TKNodeId> : IKMessageBody<TKNodeId>
        where TKNodeId : unmanaged
    {

        /// <summary>
        /// Creates a response to the given request.
        /// </summary>
        /// <param name="peers"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public KFindValueResponse<TKNodeId> Respond(KPeerEndpointInfo<TKNodeId>[] peers, in KValueInfo? value)
        {
            return new KFindValueResponse<TKNodeId>(peers, value);
        }

        /// <summary>
        /// Creates a response to the given request.
        /// </summary>
        /// <param name="peers"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public KFindValueResponse<TKNodeId> Respond(IEnumerable<KPeerEndpointInfo<TKNodeId>> peers, in KValueInfo? value)
        {
            return new KFindValueResponse<TKNodeId>(peers.ToArray(), value);
        }

        readonly TKNodeId key;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        public KFindValueRequest(in TKNodeId key)
        {
            this.key = key;
        }

        /// <summary>
        /// Specifies the key to be located.
        /// </summary>
        public TKNodeId Key => key;

    }

}
