﻿namespace Alethic.Kademlia
{

    /// <summary>
    /// Describes a response to a STORE request.
    /// </summary>
    public readonly struct KStoreResponse<TNodeId> : IKResponseBody<TNodeId>
        where TNodeId : unmanaged
    {

        readonly KStoreResponseStatus status;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="status"></param>
        public KStoreResponse(KStoreResponseStatus status)
        {
            this.status = status;
        }

        /// <summary>
        /// Gets the result of the STORE operation.
        /// </summary>
        public KStoreResponseStatus Status => status;

    }

}
