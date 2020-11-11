using System;

namespace Alethic.Kademlia
{

    /// <summary>
    /// Base class of node protocol responses.
    /// </summary>
    public readonly struct KResponse<TNodeId, TBody> : IKResponse<TNodeId, TBody>
        where TNodeId : unmanaged
        where TBody : struct, IKResponseBody<TNodeId>
    {

        readonly KMessageHeader<TNodeId> header;
        readonly KResponseStatus status;
        readonly TBody? body;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="status"></param>
        /// <param name="body"></param>
        public KResponse(in KMessageHeader<TNodeId> header, KResponseStatus status, in TBody? body)
        {
            this.header = header;
            this.status = status;
            this.body = body;
        }

        /// <summary>
        /// Gets the message header.
        /// </summary>
        public KMessageHeader<TNodeId> Header => header;

        /// <summary>
        /// Gets the status of the request.
        /// </summary>
        public KResponseStatus Status => status;

        /// <summary>
        /// Gets the response body.
        /// </summary>
        public TBody? Body => body;

        /// <summary>
        /// Gets the message body.
        /// </summary>
        IKMessageBody<TNodeId> IKMessage<TNodeId>.Body => body;

        /// <summary>
        /// Returns <c>true</c> if the object is equal to this object.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(KResponse<TNodeId, TBody> other)
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
            return other is KResponse<TNodeId, TBody> o && Equals(o);
        }

        /// <summary>
        /// Returns <c>true</c> if the object is equal to this object.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public override bool Equals(object other)
        {
            return other is KResponse<TNodeId, TBody> o && Equals(o);
        }

        /// <summary>
        /// Gets a unique hashcode for this instance.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            var h = new HashCode();
            h.Add(header);
            h.Add(status);
            h.Add(body);
            return h.ToHashCode();
        }

    }

}
