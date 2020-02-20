using System;
using System.Buffers;
using System.Buffers.Binary;

namespace Cogito.Kademlia.Network.Datagram
{

    /// <summary>
    /// Describes a sequence of <see cref="KPacketPeerEndpointInfo{TKNodeId}"/> instances.
    /// </summary>
    public readonly ref struct KPacketPeerSequence<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Provides for iteration across a <see cref="KPacketPeerSequence{TKNodeId}"/>.
        /// </summary>
        public ref struct Iterator
        {

            readonly KPacketPeerSequence<TKNodeId> sequence;
            int position;

            /// <summary>
            /// Initializes a new instance.
            /// </summary>
            /// <param name="sequence"></param>
            /// <param name="position"></param>
            public Iterator(in KPacketPeerSequence<TKNodeId> sequence, int position = 0)
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
            public KPacketPeerEndpointInfo<TKNodeId> Current => sequence[position];

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
        /// Writes out a <see cref="KPacketPeerSequence{TKNodeId}"/> structure.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="peers"></param>
        public static void Write(IBufferWriter<byte> writer, KPeerEndpointInfo<TKNodeId>[] peers)
        {
            BinaryPrimitives.WriteInt32BigEndian(writer.GetSpan(sizeof(int)), (int)peers.Length);
            writer.Advance(sizeof(int));

            for (var i = 0; i < peers.Length; i++)
                KPacketPeerEndpointInfo<TKNodeId>.Write(writer, peers[i]);
        }

        readonly ReadOnlySpan<byte> span;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="span"></param>
        public KPacketPeerSequence(ReadOnlySpan<byte> span)
        {
            this.span = span;
        }

        /// <summary>
        /// Gets the count of peers in the sequence.
        /// </summary>
        public int Count
        {
            get => (int)BinaryPrimitives.ReadUInt32BigEndian(span.Slice(0, sizeof(uint)));
        }

        /// <summary>
        /// Gets the peer at the given position.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public KPacketPeerEndpointInfo<TKNodeId> this[int index]
        {
            get => new KPacketPeerEndpointInfo<TKNodeId>(span.Slice(sizeof(uint) + index * KPacketPeerEndpointInfo<TKNodeId>));
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
