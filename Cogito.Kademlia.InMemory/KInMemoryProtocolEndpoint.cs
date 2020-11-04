using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia.InMemory
{

    /// <summary>
    /// Provides an endpoint implementation for an in memory node.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public class KInMemoryProtocolEndpoint<TNodeId> : IKProtocolEndpoint<TNodeId>, IEquatable<KInMemoryProtocolEndpoint<TNodeId>>
        where TNodeId : unmanaged
    {

        readonly IKProtocol<TNodeId> protocol;
        readonly TNodeId node;
        readonly string[] accepts;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="protocol"></param>
        /// <param name="node"></param>
        /// <param name="accepts"></param>
        public KInMemoryProtocolEndpoint(IKProtocol<TNodeId> protocol, in TNodeId node, IEnumerable<string> accepts)
        {
            this.protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            this.node = node;
            this.accepts = accepts?.ToArray();
        }

        /// <summary>
        /// Gets the associated protocol.
        /// </summary>
        public IKProtocol<TNodeId> Protocol => protocol;

        /// <summary>
        /// Gets the endpoint.
        /// </summary>
        public TNodeId Node => node;

        /// <summary>
        /// Gets the set of media types supported by the endpoint.
        /// </summary>
        public IEnumerable<string> Accepts => accepts;

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
        /// Returns a URI for the endpoint.
        /// </summary>
        /// <returns></returns>
        public unsafe Uri ToUri()
        {
            var a = new byte[KNodeId<TNodeId>.SizeOf];
            node.Write(a.AsSpan());
            return new Uri($"memory://${BitConverter.ToString(a).Replace("-", "")}");
        }

        /// <summary>
        /// Returns <c>true</c> if this instance matches the specified instance.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj is KInMemoryProtocolEndpoint<TNodeId> other && Equals(other);
        }

        /// <summary>
        /// Returns <c>true</c> if this instance matches the specified instance.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool Equals(KInMemoryProtocolEndpoint<TNodeId> other)
        {
            return ReferenceEquals(protocol, other.protocol) && node.Equals(other.node);
        }

        /// <summary>
        /// Gets a unique hash code for this instance.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            var h = new HashCode();
            h.Add(protocol);
            h.Add(node);
            return h.ToHashCode();
        }

        /// <summary>
        /// Returns a string representation of this endpoint.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return node.ToString();
        }

    }

}
