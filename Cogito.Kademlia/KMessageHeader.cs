using System;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a datagram header.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KMessageHeader<TKNodeId> : IEquatable<KMessageHeader<TKNodeId>>
        where TKNodeId : unmanaged
    {

        readonly TKNodeId sender;
        readonly ulong magic;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="magic"></param>
        public KMessageHeader(in TKNodeId sender, ulong magic)
        {
            this.sender = sender;
            this.magic = magic;
        }

        /// <summary>
        /// Gets the sender of the datagram.
        /// </summary>
        public TKNodeId Sender => sender;

        /// <summary>
        /// Gets the value identifying this datagram in a request/response lifecycle.
        /// </summary>
        public ulong Magic => magic;

        /// <summary>
        /// Returns <c>true</c> if the other object is equal to this object.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(KMessageHeader<TKNodeId> other)
        {
            return other.Sender.Equals(sender) && other.Magic.Equals(magic);
        }

        /// <summary>
        /// Returns <c>true</c> if the other object is equal to this object.
        /// </summary>
        /// <param name="obj"></param>
        public override bool Equals(object obj)
        {
            return obj is KMessageHeader<TKNodeId> other && Equals(other);
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
