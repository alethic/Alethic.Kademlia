using System;

namespace Alethic.Kademlia
{

    /// <summary>
    /// Describes a datagram header.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public readonly struct KMessageHeader<TNodeId> : IEquatable<KMessageHeader<TNodeId>>
        where TNodeId : unmanaged
    {

        readonly TNodeId sender;
        readonly uint replyId;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="replyId"></param>
        public KMessageHeader(in TNodeId sender, uint replyId)
        {
            this.sender = sender;
            this.replyId = replyId;
        }

        /// <summary>
        /// Gets the sender of the datagram.
        /// </summary>
        public TNodeId Sender => sender;

        /// <summary>
        /// Gets the value identifying this datagram in a request/response lifecycle.
        /// </summary>
        public uint ReplyId => replyId;

        /// <summary>
        /// Returns <c>true</c> if the other object is equal to this object.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(KMessageHeader<TNodeId> other)
        {
            return other.sender.Equals(sender) && other.replyId.Equals(replyId);
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
            h.Add(replyId);
            return h.ToHashCode();
        }

    }

}
