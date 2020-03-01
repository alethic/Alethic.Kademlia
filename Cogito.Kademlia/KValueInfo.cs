using System;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes the various attributes maintained along with a value.
    /// </summary>
    public readonly struct KValueInfo
    {

        readonly ReadOnlyMemory<byte> data;
        readonly ulong version;
        readonly DateTime expiration;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="version"></param>
        /// <param name="expiration"></param>
        public KValueInfo(ReadOnlyMemory<byte> data, ulong version, DateTime expiration)
        {
            this.data = data;
            this.version = version;
            this.expiration = expiration;
        }

        /// <summary>
        /// Gets the actual underlying value data.
        /// </summary>
        public ReadOnlyMemory<byte> Data => data;

        /// <summary>
        /// Gets the version of the value.
        /// </summary>
        public ulong Version => version;

        /// <summary>
        /// Gets the absolute time of expiration of the value.
        /// </summary>
        public DateTime Expiration => expiration;

    }

}
