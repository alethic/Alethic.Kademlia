using System;
using System.Collections.Generic;
using System.Linq;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a FIND_VALUE request.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KFindValueRequest<TKNodeId> : IKMessageBody<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Creates a response to the given request.
        /// </summary>
        /// <param name="peers"></param>
        /// <param name="value"></param>
        /// <param name="expiration"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        public KFindValueResponse<TKNodeId> Respond(KPeerEndpointInfo<TKNodeId>[] peers, ReadOnlyMemory<byte>? value, DateTimeOffset? expiration, ulong? version)
        {
            return new KFindValueResponse<TKNodeId>(key, peers, value, expiration, version);
        }

        /// <summary>
        /// Creates a response to the given request.
        /// </summary>
        /// <param name="peers"></param>
        /// <param name="value"></param>
        /// <param name="expiration"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        public KFindValueResponse<TKNodeId> Respond(IEnumerable<KPeerEndpointInfo<TKNodeId>> peers, ReadOnlyMemory<byte>? value, DateTimeOffset? expiration, ulong? version)
        {
            return new KFindValueResponse<TKNodeId>(key, peers.ToArray(), value, expiration, version);
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
