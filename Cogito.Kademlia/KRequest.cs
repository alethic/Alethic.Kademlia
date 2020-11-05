using System;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Defines a message.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    /// <typeparam name="TBody"></typeparam>
    public readonly struct KRequest<TNodeId, TBody> : IKRequest<TNodeId, TBody>
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
        public KRequest(in KMessageHeader<TNodeId> header, in TBody body)
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
        public TBody? Body => body;

        /// <summary>
        /// Returns <c>true</c> if the object is equal to this object.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(KRequest<TNodeId, TBody> other)
        {
            return other.Header.Equals(header) && other.Body.Equals(body);
        }

        /// <summary>
        /// Returns <c>true</c> if the object is equal to this object.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(IKMessage<TNodeId> other)
        {
            return other is KRequest<TNodeId, TBody> o && Equals(o);
        }

        /// <summary>
        /// Returns <c>true</c> if the object is equal to this object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object other)
        {
            return other is KRequest<TNodeId, TBody> o && Equals(o);
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
