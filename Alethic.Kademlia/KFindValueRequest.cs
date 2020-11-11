using System.Collections.Generic;
using System.Linq;

namespace Alethic.Kademlia
{

    /// <summary>
    /// Describes a FIND_VALUE request.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public readonly struct KFindValueRequest<TNodeId> : IKRequestBody<TNodeId>
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Creates a response to the given request.
        /// </summary>
        /// <param name="peers"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public KFindValueResponse<TNodeId> Respond(KNodeInfo<TNodeId>[] peers, in KValueInfo? value)
        {
            return new KFindValueResponse<TNodeId>(peers, value);
        }

        /// <summary>
        /// Creates a response to the given request.
        /// </summary>
        /// <param name="peers"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public KFindValueResponse<TNodeId> Respond(IEnumerable<KNodeInfo<TNodeId>> peers, in KValueInfo? value)
        {
            return new KFindValueResponse<TNodeId>(peers.ToArray(), value);
        }

        readonly TNodeId key;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        public KFindValueRequest(in TNodeId key)
        {
            this.key = key;
        }

        /// <summary>
        /// Specifies the key to be located.
        /// </summary>
        public TNodeId Key => key;

    }

}
