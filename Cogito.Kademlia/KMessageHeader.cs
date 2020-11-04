using System;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a datagram header.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public readonly struct KMessageHeader<TNodeId> : IEquatable<KMessageHeader<TNodeId>>
        where TNodeId : unmanaged
    {

        readonly TNodeId sender;
        readonly ulong magic;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="magic"></param>
        public KMessageHeader(in TNodeId sender, ulong magic)
        {
            this.sender = sender;
            this.magic = magic;
        }

        /// <summary>
        /// Gets the sender of the datagram.
        /// </summary>
        public TNodeId Sender => sender;

        /// <summary>
        /// Gets the value identifying this datagram in a request/response lifecycle.
        /// </summary>
        public ulong Magic => magic;

        /// <summary>
        /// Returns <c>true</c> if the other object is equal to this object.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(KMessageHeader<TNodeId> other)
        {
            return other.Sender.Equals(sender) && other.Magic.Equals(magic);
        }

        /// <summary>
        /// Returns <c>true</c> if the other object is equal to this object.
        /// </summary>
        /// <param name="obj"></param>
        public override bool Equals(object obj)
        {
            return obj is KMessageHeader<TNodeId> other && Equals(other);
        }

        /// <summary>
        /// Returns a unique hashcode for this instance.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            var h = new HashCode();
            h.Add(sender);
            h.Add(magic);
            return h.ToHashCode();
        }

    }

}
