using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia.Network
{

    /// <summary>
    /// Provides a simple IP endpoint connected to a protocol.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KIpProtocolEndpoint<TKNodeId> : IKEndpoint<TKNodeId>, IEquatable<KIpProtocolEndpoint<TKNodeId>>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly IKProtocol<TKNodeId> protocol;
        readonly KIpEndpoint endpoint;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="protocol"></param>
        /// <param name="endpoint"></param>
        public KIpProtocolEndpoint(IKProtocol<TKNodeId> protocol, in KIpEndpoint endpoint)
        {
            this.protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            this.endpoint = endpoint;
        }

        /// <summary>
        /// Gets the associated protocol.
        /// </summary>
        public IKProtocol<TKNodeId> Protocol => protocol;

        /// <summary>
        /// Gets the unique identifier of the protocol available over this endpoint.
        /// </summary>
        public Guid ProtocolId => protocol.Id;

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
        /// Returns <c>true</c> if this instance matches the specified instance.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj is KIpProtocolEndpoint<TKNodeId> other && Equals(other);
        }

        /// <summary>
        /// Returns <c>true</c> if this instance matches the specified instance.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool Equals(KIpProtocolEndpoint<TKNodeId> other)
        {
            return ReferenceEquals(protocol, other.protocol) && endpoint.Equals(other.endpoint);
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
            return h.ToHashCode();
        }

        /// <summary>
        /// Returns a string representation of this endpoint.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return endpoint.ToString();
        }

    }

}
