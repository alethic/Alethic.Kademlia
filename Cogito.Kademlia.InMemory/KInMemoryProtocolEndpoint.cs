using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia.InMemory
{

    /// <summary>
    /// Provides an endpoint implementation for an in memory node.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public class KInMemoryProtocolEndpoint<TKNodeId> : IKEndpoint<TKNodeId>, IEquatable<KInMemoryProtocolEndpoint<TKNodeId>>
        where TKNodeId : unmanaged
    {

        readonly IKProtocol<TKNodeId> protocol;
        readonly TKNodeId node;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="protocol"></param>
        /// <param name="node"></param>
        public KInMemoryProtocolEndpoint(IKProtocol<TKNodeId> protocol, in TKNodeId node)
        {
            this.protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            this.node = node;
        }

        /// <summary>
        /// Gets the associated protocol.
        /// </summary>
        public IKProtocol<TKNodeId> Protocol => protocol;

        /// <summary>
        /// Gets the endpoint.
        /// </summary>
        public TKNodeId Node => node;

        /// <summary>
        /// Initiates a PING request against the endpoint.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TKNodeId, KPingResponse<TKNodeId>>> PingAsync(in KPingRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            return protocol.PingAsync(this, request, cancellationToken);
        }

        /// <summary>
        /// Initiates a STORE request against the endpoint.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TKNodeId, KStoreResponse<TKNodeId>>> StoreAsync(in KStoreRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            return protocol.StoreAsync(this, request, cancellationToken);
        }

        /// <summary>
        /// Initiates a FIND_NODE request against the endpoint.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>> FindNodeAsync(in KFindNodeRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            return protocol.FindNodeAsync(this, request, cancellationToken);
        }

        /// <summary>
        /// Initiates a FIND_VALUE request against the endpoint.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TKNodeId, KFindValueResponse<TKNodeId>>> FindValueAsync(in KFindValueRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            return protocol.FindValueAsync(this, request, cancellationToken);
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
        /// Returns <c>true</c> if this instance matches the specified instance.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj is KInMemoryProtocolEndpoint<TKNodeId> other && Equals(other);
        }

        /// <summary>
        /// Returns <c>true</c> if this instance matches the specified instance.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool Equals(KInMemoryProtocolEndpoint<TKNodeId> other)
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
