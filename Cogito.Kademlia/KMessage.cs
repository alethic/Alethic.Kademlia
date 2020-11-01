using System;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Defines a message.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    /// <typeparam name="TBody"></typeparam>
    public readonly struct KMessage<TKNodeId, TBody> : IKMessage<TKNodeId>
        where TKNodeId : unmanaged
        where TBody : struct, IKMessageBody<TKNodeId>
    {

        readonly KMessageHeader<TKNodeId> header;
        readonly TBody body;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="header"></param>
        /// <param name="body"></param>
        public KMessage(in KMessageHeader<TKNodeId> header, in TBody body)
        {
            this.header = header;
            this.body = body;
        }

        /// <summary>
        /// Gets the message header.
        /// </summary>
        public KMessageHeader<TKNodeId> Header => header;

        /// <summary>
        /// Gets the message body.
        /// </summary>
        public TBody Body => body;

        /// <summary>
        /// Gets the message body.
        /// </summary>
        IKMessageBody<TKNodeId> IKMessage<TKNodeId>.Body => body;

        /// <summary>
        /// Returns <c>true</c> if the object is equal to this object.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(IKMessage<TKNodeId> other)
        {
            return other.Header.Equals(header) && other.Body.Equals(body);
        }

        /// <summary>
        /// Returns <c>true</c> if the object is equal to this object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj is IKMessage<TKNodeId> other && Equals(other);
        }

        /// <summary>
        /// Gets a unique hashcode for this instance.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            var h = new HashCode();
            h.Add(header);
            h.Add(body);
            return h.ToHashCode();
        }

    }

}
