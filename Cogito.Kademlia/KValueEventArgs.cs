using System;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes an event that occurred to a value.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public class KValueEventArgs<TKNodeId> : EventArgs
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly TKNodeId key;
        readonly KValueInfo? value;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public KValueEventArgs(in TKNodeId key, in KValueInfo? value)
        {
            this.key = key;
            this.value = value;
        }

        /// <summary>
        /// Gets the key that was changed.
        /// </summary>
        public TKNodeId Key => key;

        /// <summary>
        /// Gets the new value information.
        /// </summary>
        public KValueInfo? Value => value;

    }

}
