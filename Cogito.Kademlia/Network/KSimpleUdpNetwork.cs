using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Cogito.Kademlia.Network
{

    /// <summary>
    /// Implements a simple UDP network layer.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    /// <typeparam name="TKPeerData"></typeparam>
    public class KSimpleUdpNetwork<TKNodeId, TKPeerData> :
        IKProtocol<TKNodeId, TKPeerData>
        where TKNodeId : struct, IKNodeId<TKNodeId>
        where TKPeerData : IKIpEndpointProvider
    {

        readonly IPAddress address;
        readonly ushort port;

        Socket socket;
        SocketAsyncEventArgs recvArgs;
        SocketAsyncEventArgs sendArgs;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public KSimpleUdpNetwork(IPAddress address, ushort port)
        {
            this.address = address;
            this.port = port;
        }

        /// <summary>
        /// Starts the network.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            socket = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, true);
            socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            socket.Bind(new IPEndPoint(address, port));

            recvArgs = new SocketAsyncEventArgs();
            recvArgs.SetBuffer(new byte[8192], 0, 8192);
            recvArgs.Completed += recvArgs_Completed;

            sendArgs = new SocketAsyncEventArgs();
            sendArgs.SetBuffer(new byte[8192], 0, 8192);
            sendArgs.Completed += sendArgs_Completed;

            socket.ReceiveAsync(recvArgs);
        }

        /// <summary>
        /// Invoked when a receive operation completes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void recvArgs_Completed(object sender, SocketAsyncEventArgs args)
        {
            if (args.LastOperation != SocketAsyncOperation.Receive)
                return;

            var r = new ReadOnlySpan<byte>(args.Buffer, 0, args.BytesTransferred);
            var m = MemoryPool<byte>.Shared.Rent(args.BytesTransferred);
            var w = m.Memory.Span.Slice(0, args.BytesTransferred);
            r.CopyTo(w);
            Task.Run(async () => { try { await OnReceiveAsync(m.Memory.Span.Slice(0, args.BytesTransferred)); } finally { m.Dispose(); } });
            socket.ReceiveAsync(recvArgs);
        }

        /// <summary>
        /// Invoked when a datagram is received.
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        Task OnReceiveAsync(ReadOnlySpan<byte> packet)
        {
            return Task.CompletedTask;
        }

        void sendArgs_Completed(object sender, SocketAsyncEventArgs args)
        {

        }

        /// <summary>
        /// Stops the network.
        /// </summary>
        /// <returns></returns>
        public async Task StopAsync()
        {
            if (socket != null)
            {
                try
                {
                    socket.Close();
                    socket.Dispose();
                }
                catch (ObjectDisposedException)
                {

                }
                finally
                {
                    socket = null;
                }
            }
        }

        public ValueTask<KNodePingResponse> PingAsync(TKNodeId nodeId, TKPeerData peerData, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ValueTask<KNodeStoreResponse> StoreAsync(TKNodeId nodeId, TKPeerData peerData, TKNodeId key, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ValueTask<KNodeFindNodeResponse> FindNodeAsync(TKNodeId nodeId, TKPeerData peerData, TKNodeId key, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ValueTask<KNodeFindValueResponse> FindValueAsync(TKNodeId nodeId, TKPeerData peerData, TKNodeId key, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

    }

}
