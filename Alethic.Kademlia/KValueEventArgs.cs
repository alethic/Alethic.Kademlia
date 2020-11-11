using System;

namespace Alethic.Kademlia
{

    /// <summary>
    /// Describes an event that occurred to a value.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public class KValueEventArgs<TNodeId> : EventArgs
        where TNodeId : unmanaged
    {

        readonly TNodeId key;
        readonly KValueInfo? value;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public KValueEventArgs(in TNodeId key, in KValueInfo? value)
        {
            this.key = key;
            this.value = value;
        }

        /// <summary>
        /// Gets the key that was changed.
        /// </summary>
        public TNodeId Key => key;

        /// <summary>
        /// Gets the new value information.
        /// </summary>
        public KValueInfo? Value => value;

    }

}
