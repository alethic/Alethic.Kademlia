using System.Collections.Generic;
using System.Linq;

namespace Alethic.Kademlia
{

    /// <summary>
    /// Describes a FIND_NODE request.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public readonly struct KFindNodeRequest<TNodeId> : IKRequestBody<TNodeId>
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Creates a response to the given request.
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        public KFindNodeResponse<TNodeId> Respond(KNodeInfo<TNodeId>[] nodes)
        {
            return new KFindNodeResponse<TNodeId>(nodes);
        }

        /// <summary>
        /// Creates a response to the given request.
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        public KFindNodeResponse<TNodeId> Respond(IEnumerable<KNodeInfo<TNodeId>> nodes)
        {
            return new KFindNodeResponse<TNodeId>(nodes.ToArray());
        }

        readonly TNodeId key;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        public KFindNodeRequest(in TNodeId key)
        {
            this.key = key;
        }

        /// <summary>
        /// Specifies the node ID to be located.
        /// </summary>
        public TNodeId Key => key;

    }

}
