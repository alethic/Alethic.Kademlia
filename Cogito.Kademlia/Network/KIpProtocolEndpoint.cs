using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia.Network
{

    /// <summary>
    /// Provides a simple IP endpoint connected to a protocol.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public struct KIpProtocolEndpoint<TKNodeId> : IKEndpoint<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly IKProtocol<TKNodeId> protocol;
        readonly TKNodeId nodeId;
        readonly KIpEndpoint endpoint;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="protocol"></param>
        /// <param name="nodeId"></param>
        /// <param name="endpoint"></param>
        public KIpProtocolEndpoint(IKProtocol<TKNodeId> protocol, in TKNodeId nodeId, in KIpEndpoint endpoint)
        {
            this.protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            this.nodeId = nodeId;
            this.endpoint = endpoint;
        }

        /// <summary>
        /// Gets the associated protocol.
        /// </summary>
        public IKProtocol<TKNodeId> Protocol => protocol;

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
            return protocol.PingAsync(nodeId, this, request, cancellationToken);
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
            return protocol.StoreAsync(nodeId, this, request, cancellationToken);
        }

        /// <summary>
        /// Initiates a FIND_NODE request against the endpoint.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>> FindNodeAsync(in KFindNodeRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            return protocol.FindNodeAsync(nodeId, this, request, cancellationToken);
        }

        /// <summary>
        /// Initiates a FIND_VALUE request against the endpoint.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TKNodeId, KFindValueResponse<TKNodeId>>> FindValueAsync(in KFindValueRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            return protocol.FindValueAsync(nodeId, this, request, cancellationToken);
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
