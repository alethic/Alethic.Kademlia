using System;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Defines a message.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    /// <typeparam name="TBody"></typeparam>
    public readonly struct KMessage<TNodeId, TBody> : IKMessage<TNodeId>
        where TNodeId : unmanaged
        where TBody : struct, IKRequestBody<TNodeId>
    {

        readonly KMessageHeader<TNodeId> header;
        readonly TBody body;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="header"></param>
        /// <param name="body"></param>
        public KMessage(in KMessageHeader<TNodeId> header, in TBody body)
        {
            this.header = header;
            this.body = body;
        }

        /// <summary>
        /// Gets the message header.
        /// </summary>
        public KMessageHeader<TNodeId> Header => header;

        /// <summary>
        /// Gets the message body.
        /// </summary>
        public TBody Body => body;

        /// <summary>
        /// Gets the message body.
        /// </summary>
        IKRequestBody<TNodeId> IKMessage<TNodeId>.Body => body;

        /// <summary>
        /// Returns <c>true</c> if the object is equal to this object.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(IKMessage<TNodeId> other)
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
            return obj is IKMessage<TNodeId> other && Equals(other);
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
