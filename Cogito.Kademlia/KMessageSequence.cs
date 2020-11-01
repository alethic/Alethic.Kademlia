using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a sequence of messages that have been decoded.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KMessageSequence<TKNodeId> : IEnumerable<IKMessage<TKNodeId>>, IEquatable<KMessageSequence<TKNodeId>>
        where TKNodeId : unmanaged
    {

        readonly ulong network;
        readonly IKMessage<TKNodeId>[] messages;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="network"></param>
        /// <param name="messages"></param>
        public KMessageSequence(ulong network, IKMessage<TKNodeId>[] messages)
        {
            this.network = network;
            this.messages = messages ?? throw new ArgumentNullException(nameof(messages));
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="network"></param>
        /// <param name="messages"></param>
        public KMessageSequence(ulong network, IEnumerable<IKMessage<TKNodeId>> messages) :
            this(network, messages?.ToArray())
        {

        }

        /// <summary>
        /// Gets the network ID of the message sequence.
        /// </summary>
        public ulong Network => network;

        /// <summary>
        /// Returns <c>true</c> if this object equals the other object.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(KMessageSequence<TKNodeId> other)
        {
            return other.Network == network && other.messages.SequenceEqual(messages);
        }

        /// <summary>
        /// Returns <c>true</c> if this object equals the other object.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj is KMessageSequence<TKNodeId> other && Equals(other);
        }

        /// <summary>
        /// Returns a unique hashcode for this object.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            var h = new HashCode();
            h.Add(network);
            h.Add(messages.Length);

            foreach (var m in messages)
                h.Add(m);

            return h.ToHashCode();
        }

        /// <summary>
        /// Gets an enumerator of the messages.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<IKMessage<TKNodeId>> GetEnumerator()
        {
            return ((IEnumerable<IKMessage<TKNodeId>>)messages).GetEnumerator();
        }

        /// <summary>
        /// Gets an enumerator of the messages.
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }

}
