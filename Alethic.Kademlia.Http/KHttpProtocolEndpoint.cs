using System;
using System.Threading;
using System.Threading.Tasks;

namespace Alethic.Kademlia.Http
{

    /// <summary>
    /// Represents an endpoint using the HTTP protocol.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public class KHttpProtocolEndpoint<TNodeId> : IKProtocolEndpoint<TNodeId>
        where TNodeId : unmanaged
    {

        readonly KHttpProtocol<TNodeId> protocol;
        readonly Uri uri;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="protocol"></param>
        /// <param name="uri"></param>
        public KHttpProtocolEndpoint(KHttpProtocol<TNodeId> protocol, Uri uri)
        {
            this.protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            this.uri = uri ?? throw new ArgumentNullException(nameof(uri));

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidOperationException();
        }

        /// <summary>
        /// Invokes the given request against this endpoint.
        /// </summary>
        /// <typeparam name="TRequestBody"></typeparam>
        /// <typeparam name="TResponseBody"></typeparam>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TNodeId, TResponseBody>> InvokeAsync<TRequestBody, TResponseBody>(in TRequestBody request, CancellationToken cancellationToken)
            where TRequestBody : struct, IKRequestBody<TNodeId>
            where TResponseBody : struct, IKResponseBody<TNodeId>
        {
            return protocol.InvokeAsync<TRequestBody, TResponseBody>(this, request, cancellationToken);
        }

        /// <summary>
        /// Returns a <see cref="Uri"/> representation of this endpoint.
        /// </summary>
        /// <returns></returns>
        public Uri ToUri()
        {
            return uri;
        }

    }

}
