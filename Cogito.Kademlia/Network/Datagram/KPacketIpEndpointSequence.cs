using System;
using System.Buffers;
using System.Buffers.Binary;

namespace Cogito.Kademlia.Network.Datagram
{

    /// <summary>
    /// Describes a sequence of <see cref="KIpEndpoint"/> instances.
    /// </summary>
    public readonly ref struct KPacketIpEndpointSequence
    {

        /// <summary>
        /// Provides for iteration across a <see cref="KPacketIpEndpointSequence"/>.
        /// </summary>
        public ref struct Iterator
        {

            readonly KPacketIpEndpointSequence sequence;
            int position;

            /// <summary>
            /// Initializes a new instance.
            /// </summary>
            /// <param name="sequence"></param>
            /// <param name="position"></param>
            public Iterator(in KPacketIpEndpointSequence sequence, int position = 0)
            {
                this.sequence = sequence;
                this.position = position;
            }

            /// <summary>
            /// Gets the iterator.
            /// </summary>
            /// <returns></returns>
            public Iterator GetEnumerator() => new Iterator(sequence, position);

            /// <summary>
            /// Gets the current value.
            /// </summary>
            public KPacketIpEndpoint Current => sequence[position];

            /// <summary>
            /// Moves to the next value.
            /// </summary>
            /// <returns></returns>
            public bool MoveNext()
            {
                if (position < sequence.Count)
                {
                    position++;
                    return true;
                }
                else
                {
                    return false;
                }
            }

        }

        /// <summary>
        /// Writes out a <see cref="KPacketIpEndpointSequence"/> structure.
        /// </summary>
        /// <typeparam name="TKNodeId"></typeparam>
        /// <param name="writer"></param>
        /// <param name="endpoints"></param>
        public static void Write<TKNodeId>(IBufferWriter<byte> writer, KIpProtocolEndpoint<TKNodeId>[] endpoints) where TKNodeId : unmanaged, IKNodeId<TKNodeId>
        {
            BinaryPrimitives.WriteUInt32BigEndian(writer.GetSpan(sizeof(uint)), (uint)endpoints.Length);
            writer.Advance(sizeof(uint));

            for (var i = 0; i < endpoints.Length; i++)
                KPacketIpEndpoint.Write(writer, endpoints[i]);
        }

        readonly ReadOnlySpan<byte> span;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="span"></param>
        public KPacketIpEndpointSequence(ReadOnlySpan<byte> span)
        {
            this.span = span;
        }

        /// <summary>
        /// Gets the count of endpoints in the sequence.
        /// </summary>
        public int Count
        {
            get => BinaryPrimitives.ReadInt32BigEndian(span.Slice(0, sizeof(int)));
        }

        /// <summary>
        /// Gets the IP endpoint at the given position.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public KPacketIpEndpoint this[int index]
        {
            get => new KPacketIpEndpoint(span.Slice(sizeof(int) + index * KPacketIpEndpointInfo.Size, KPacketIpEndpointInfo.Size));
        }

        /// <summary>
        /// Gets an enumerator over the items of the sequence.
        /// </summary>
        /// <returns></returns>
        public Iterator GetEnumerable()
        {
            return new Iterator(this);
        }

    }

}
