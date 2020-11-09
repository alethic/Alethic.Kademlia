using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia.Network
{

    /// <summary>
    /// Provides a simple IP endpoint connected to a protocol.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public class KIpProtocolEndpoint<TNodeId> : IKProtocolEndpoint<TNodeId>, IEquatable<KIpProtocolEndpoint<TNodeId>>
        where TNodeId : unmanaged
    {

        readonly IKIpProtocol<TNodeId> protocol;
        readonly KIpEndpoint endpoint;
        readonly KIpProtocolType type;
        readonly IEnumerable<string> formats;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="protocol"></param>
        /// <param name="endpoint"></param>
        /// <param name="type"></param>
        /// <param name="formats"></param>
        public KIpProtocolEndpoint(IKIpProtocol<TNodeId> protocol, in KIpEndpoint endpoint, KIpProtocolType type, IEnumerable<string> formats)
        {
            this.protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            this.endpoint = endpoint;
            this.type = type;
            this.formats = formats ?? throw new ArgumentNullException(nameof(formats));
        }

        /// <summary>
        /// Gets the associated protocol.
        /// </summary>
        public IKProtocol<TNodeId> Protocol => protocol;

        /// <summary>
        /// Gets the set of format types supported by the endpoint.
        /// </summary>
        public IEnumerable<string> Formats => formats;

        /// <summary>
        /// Gets the endpoint.
        /// </summary>
        public KIpEndpoint Endpoint => endpoint;

        /// <summary>
        /// Initiates a PING request against the endpoint.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TNodeId, TResponse>> InvokeAsync<TRequest, TResponse>(in TRequest request, CancellationToken cancellationToken)
            where TRequest : struct, IKRequestBody<TNodeId>
            where TResponse : struct, IKResponseBody<TNodeId>
        {
            return protocol.InvokeAsync<TRequest, TResponse>(this, request, cancellationToken);
        }

        /// <summary>
        /// Raised when a success is received.
        /// </summary>
        public event EventHandler<KEndpointSuccessEventArgs> Success;

        /// <summary>
        /// Raises the Success event.
        /// </summary>
        /// <param name="args"></param>
        public void OnSuccess(KEndpointSuccessEventArgs args)
        {
            Success?.Invoke(this, args);
        }

        /// <summary>
        /// Raised when a timeout is received.
        /// </summary>
        public event EventHandler<KEndpointTimeoutEventArgs> Timeout;

        /// <summary>
        /// Raises the Timeout event.
        /// </summary>
        /// <param name="args"></param>
        public void OnTimeout(KEndpointTimeoutEventArgs args)
        {
            Timeout?.Invoke(this, args);
        }

        /// <summary>
        /// Generates a URI for the endpoint.
        /// </summary>
        /// <returns></returns>
        public Uri ToUri()
        {
            var q = "?format=" + string.Join(",", formats);
            var h = endpoint.Protocol switch { KIpAddressFamily.IPv4 => endpoint.V4.ToString(), KIpAddressFamily.IPv6 => endpoint.V6.ToString() };
            Uri F(string protocol) => new UriBuilder() { Scheme = protocol, Host = h, Port = endpoint.Port, Query = q }.Uri;

            return type switch
            {
                KIpProtocolType.Udp => F("udp"),
                KIpProtocolType.Tcp => F("tcp"),
                _ => throw new InvalidOperationException(),
            };
        }

        /// <summary>
        /// Returns <c>true</c> if this instance matches the specified instance.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj is KIpProtocolEndpoint<TNodeId> other && Equals(other);
        }

        /// <summary>
        /// Returns <c>true</c> if this instance matches the specified instance.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool Equals(KIpProtocolEndpoint<TNodeId> other)
        {
            return ReferenceEquals(protocol, other.protocol) && endpoint.Equals(other.endpoint) && formats.SequenceEqual(other.formats);
        }

        /// <summary>
        /// Gets a unique hash code for this instance.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            var h = new HashCode();
            h.Add(protocol);
            h.Add(endpoint);

            var i = 0;
            foreach (var a in formats)
            {
                h.Add(a);
                i++;
            }

            h.Add(i);

            return h.ToHashCode();
        }

        /// <summary>
        /// Returns a string representation of this endpoint.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToUri().ToString();
        }

    }

}