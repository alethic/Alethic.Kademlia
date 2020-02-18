using System;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a response to a FIND_VALUE request.
    /// </summary>
    public readonly struct KFindValueResponse<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly TKNodeId key;
        readonly ReadOnlyMemory<byte> value;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public KFindValueResponse(in TKNodeId key, ReadOnlyMemory<byte> value)
        {
            this.key = key;
            this.value = value;
        }

        /// <summary>
        /// Gets the key to locate the value of.
        /// </summary>
        public TKNodeId Key => key;

        /// <summary>
        /// Gets the value that was located.
        /// </summary>
        public ReadOnlyMemory<byte> Value => value;

    }

}
