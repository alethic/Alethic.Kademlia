using System;
using System.Buffers;

namespace Cogito.Kademlia.Network.Protocol.Datagram
{

    /// <summary>
    /// Describes the data to send for a FIND_VALUE response.
    /// </summary>
    public readonly ref struct KFindValueResponseBody<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly TKNodeId key;
        readonly ReadOnlySpan<byte> value;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public KFindValueResponseBody(in TKNodeId key, ReadOnlySpan<byte> value)
        {
            this.key = key;
            this.value = value;
        }

        /// <summary>
        /// Gets or sets the key that was found.
        /// </summary>
        public TKNodeId Key => key;

        /// <summary>
        /// Gets or sets the value that was found.
        /// </summary>
        public ReadOnlySpan<byte> Value => value;

        /// <summary>
        /// Writes the data to the specified buffer writer.
        /// </summary>
        /// <param name="writer"></param>
        public unsafe void WriteTo(IBufferWriter<byte> writer)
        {
            key.WriteTo(writer);
            writer.Write(value);
        }

    }

}
