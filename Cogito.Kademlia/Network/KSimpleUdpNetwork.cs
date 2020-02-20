using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Cogito.Kademlia.Core;
using Cogito.Kademlia.Network.Datagram;
using Cogito.Threading;

namespace Cogito.Kademlia.Network
{

    /// <summary>
    /// Implements a simple UDP network layer.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    /// <typeparam name="TKPeerData"></typeparam>
    public class KSimpleUdpNetwork<TKNodeId, TKPeerData> : IKIpProtocol<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
        where TKPeerData : IKEndpointProvider<TKNodeId>
    {

        static readonly Guid ID = new Guid("3B8AA87F-1C81-4B4E-805F-5996F06DDCC5");
        static readonly TimeSpan DEFAULT_TIMEOUT = TimeSpan.FromSeconds(30);
        static readonly Random rnd = new Random();

        readonly IKEngine<TKNodeId, TKPeerData> engine;
        readonly ushort port;
        readonly AsyncLock sync = new AsyncLock();

        Socket socket;
        SocketAsyncEventArgs recvArgs;
        SocketAsyncEventArgs sendArgs;
        Dictionary<KIpEndpoint, KIpProtocolEndpoint<TKNodeId>> endpoints = new Dictionary<KIpEndpoint, KIpProtocolEndpoint<TKNodeId>>();

        KIpResponseQueue<TKNodeId, KPingResponse<TKNodeId>> pingQueue = new KIpResponseQueue<TKNodeId, KPingResponse<TKNodeId>>(DEFAULT_TIMEOUT);
        KIpResponseQueue<TKNodeId, KStoreResponse<TKNodeId>> storeQueue = new KIpResponseQueue<TKNodeId, KStoreResponse<TKNodeId>>(DEFAULT_TIMEOUT);
        KIpResponseQueue<TKNodeId, KFindNodeResponse<TKNodeId>> findNodeQueue = new KIpResponseQueue<TKNodeId, KFindNodeResponse<TKNodeId>>(DEFAULT_TIMEOUT);
        KIpResponseQueue<TKNodeId, KFindValueResponse<TKNodeId>> findValueQueue = new KIpResponseQueue<TKNodeId, KFindValueResponse<TKNodeId>>(DEFAULT_TIMEOUT);

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="port"></param>
        public KSimpleUdpNetwork(IKEngine<TKNodeId, TKPeerData> engine, ushort port = 0)
        {
            this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
            this.port = port;
        }

        /// <summary>
        /// Gets the engine associated with this protocol.
        /// </summary>
        public IKEngine<TKNodeId> Engine => engine;

        /// <summary>
        /// Gets the unique identifier of this protocol.
        /// </summary>
        public Guid Id => ID;

        /// <summary>
        /// Gets the port number either currently listening on, or configured to listen on.
        /// </summary>
        public ushort Port => socket != null ? (ushort)((IPEndPoint)socket.LocalEndPoint).Port : port;

        /// <summary>
        /// Gets the set of endpoints through which this protocol is reachable.
        /// </summary>
        public IEnumerable<IKEndpoint<TKNodeId>> Endpoints => endpoints.Values.Cast<IKEndpoint<TKNodeId>>();

        /// <summary>
        /// Creates a new <see cref="KIpProtocolEndpoint{TKNodeId}"/> for the given <see cref="KIpEndpoint"/>.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public KIpProtocolEndpoint<TKNodeId> CreateEndpoint(in KIpEndpoint endpoint)
        {
            return new KIpProtocolEndpoint<TKNodeId>(this, endpoint);
        }

        /// <summary>
        /// Gets the next magic value.
        /// </summary>
        /// <returns></returns>
        uint GetNextMagic()
        {
            return (uint)rnd.Next(int.MinValue, int.MaxValue);
        }

        /// <summary>
        /// Gets the local available IP addresses.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IPAddress> GetLocalIpAddresses()
        {
            if (NetworkInterface.GetIsNetworkAvailable())
                foreach (var i in NetworkInterface.GetAllNetworkInterfaces())
                    if (i.OperationalStatus == OperationalStatus.Up)
                        foreach (var j in i.GetIPProperties().UnicastAddresses)
                            if (IPAddress.IsLoopback(j.Address) == false)
                                yield return j.Address;
        }

        /// <summary>
        /// Gets the local endpoints.
        /// </summary>
        /// <returns></returns>
        IEnumerable<KIpEndpoint> GetLocalIpEndpoints(Socket socket)
        {
            foreach (var i in GetLocalIpAddresses())
            {
                switch (i.AddressFamily)
                {
                    case AddressFamily.InterNetwork:
                        yield return new KIpEndpoint(new KIp4Address(i), port != 0 ? port : ((IPEndPoint)socket.LocalEndPoint).Port);
                        break;
                    case AddressFamily.InterNetworkV6:
                        yield return new KIpEndpoint(new KIp6Address(i), port != 0 ? port : ((IPEndPoint)socket.LocalEndPoint).Port);
                        break;
                }
            }
        }

        /// <summary>
        /// Starts the network.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            using (await sync.LockAsync())
            {
                // establish UDP socket
                socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                socket.DualMode = true;
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, true);
                socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.PacketInformation, true);
                socket.Bind(new IPEndPoint(IPAddress.IPv6Any, port));

                // reset local endpoint intelligence
                endpoints.Clear();
                foreach (var ip in GetLocalIpEndpoints(socket))
                    endpoints[ip] = CreateEndpoint(ip);

                // remove any endpoints of ourselves
                foreach (var ep in endpoints.Values)
                    engine.SelfData.Endpoints.Remove(ep);

                // add new endpoints to engine
                foreach (var ep in endpoints.Values)
                    engine.SelfData.Endpoints.Add(ep);

                recvArgs = new SocketAsyncEventArgs();
                recvArgs.RemoteEndPoint = new IPEndPoint(IPAddress.IPv6Any, 0);
                recvArgs.SetBuffer(new byte[8192], 0, 8192);
                recvArgs.Completed += recvArgs_Completed;

                sendArgs = new SocketAsyncEventArgs();
                sendArgs.SetBuffer(new byte[8192], 0, 8192);
                sendArgs.Completed += sendArgs_Completed;

                socket.ReceiveMessageFromAsync(recvArgs);
            }
        }

        /// <summary>
        /// Invoked when a receive operation completes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void recvArgs_Completed(object sender, SocketAsyncEventArgs args)
        {
            if (args.LastOperation != SocketAsyncOperation.ReceiveMessageFrom)
                return;

            var p = new KIpEndpoint((IPEndPoint)args.RemoteEndPoint);
            var b = new ReadOnlySpan<byte>(args.Buffer, args.Offset, args.Count);
            var o = MemoryPool<byte>.Shared.Rent(b.Length);
            var m = o.Memory.Slice(0, b.Length);
            b.CopyTo(m.Span);
            Task.Run(async () => { try { await OnReceiveAsync(p, m.Span, CancellationToken.None); } catch { } finally { o.Dispose(); } });

            // continue receiving if socket still available
            var s = socket;
            if (s != null && s.IsBound)
            {
                recvArgs.RemoteEndPoint = new IPEndPoint(IPAddress.IPv6Any, 0);
                socket.ReceiveMessageFromAsync(recvArgs);
            }
        }

        /// <summary>
        /// Invoked when a datagram is received.
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        ValueTask OnReceiveAsync(in KIpEndpoint endpoint, ReadOnlySpan<byte> packet, CancellationToken cancellationToken)
        {
            // check for continued connection
            var s = socket;
            if (s == null || s.IsBound == false)
                return new ValueTask(Task.CompletedTask);

            // read header and advance past
            var header = new KPacketHeaderReadOnly<TKNodeId>(packet);
            packet = packet.Slice(KPacketHeaderInfo<TKNodeId>.Size);

            // route based on packet type
            return header.Type switch
            {
                KPacketType.PingRequest => OnReceivePingRequestAsync(endpoint, header, packet, cancellationToken),
                KPacketType.PingResponse => OnReceivePingResponseAsync(endpoint, header, packet, cancellationToken),
                KPacketType.StoreRequest => OnReceiveStoreRequestAsync(endpoint, header, packet, cancellationToken),
                KPacketType.StoreResponse => OnReceiveStoreResponseAsync(endpoint, header, packet, cancellationToken),
                KPacketType.FindNodeRequest => OnReceiveFindNodeRequestAsync(endpoint, header, packet, cancellationToken),
                KPacketType.FindNodeResponse => OnReceiveFindNodeResponseAsync(endpoint, header, packet, cancellationToken),
                KPacketType.FindValueRequest => OnReceiveFindValueRequestAsync(endpoint, header, packet, cancellationToken),
                KPacketType.FindValueResponse => OnReceiveFindValueResponseAsync(endpoint, header, packet, cancellationToken),
                _ => new ValueTask(Task.CompletedTask),
            };
        }

        /// <summary>
        /// Invoked when a PING request is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="header"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        ValueTask OnReceivePingRequestAsync(in KIpEndpoint endpoint, in KPacketHeaderReadOnly<TKNodeId> header, ReadOnlySpan<byte> packet, CancellationToken cancellationToken)
        {
            return OnReceivePingRequestAsync(endpoint, header, new KPacketPingRequest<TKNodeId>(packet), cancellationToken);
        }

        ValueTask OnReceivePingRequestAsync(in KIpEndpoint endpoint, in KPacketHeaderReadOnly<TKNodeId> header, in KPacketPingRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            var endpoints = new IKEndpoint<TKNodeId>[request.Endpoints.Count];
            for (var i = 0; i < endpoints.Length; i++)
                endpoints[i] = CreateEndpoint(request.Endpoints[i].Value);

            return OnReceivePingRequestAsync(endpoint, header.Sender, header.Magic, new KPingRequest<TKNodeId>(endpoints), cancellationToken);
        }

        async ValueTask OnReceivePingRequestAsync(KIpEndpoint endpoint, TKNodeId sender, uint magic, KPingRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            await OnPingReplyAsync(endpoint, sender, magic, await engine.OnPingAsync(sender, request, cancellationToken), cancellationToken);
        }

        ValueTask OnPingReplyAsync(in KIpEndpoint endpoint, in TKNodeId sender, uint magic, in KPingResponse<TKNodeId> response, CancellationToken cancellationToken)
        {
            var b = new ArrayBufferWriter<byte>();
            var h = new KPacketHeader<TKNodeId>(b.GetSpan(KPacketHeaderInfo<TKNodeId>.Size));
            h.Version = 1;
            h.Sender = engine.SelfId;
            h.Magic = magic;
            h.Type = KPacketType.PingResponse;
            KPacketPingResponse<TKNodeId>.Write(b, response);

            var z = new byte[b.WrittenCount];
            b.WrittenSpan.CopyTo(z);
            return new ValueTask(socket.SendToAsync(new ArraySegment<byte>(z), SocketFlags.None, endpoint.ToIPEndPoint()));
        }

        /// <summary>
        /// Invoked when a STORE request is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="header"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        ValueTask OnReceiveStoreRequestAsync(in KIpEndpoint endpoint, in KPacketHeaderReadOnly<TKNodeId> header, ReadOnlySpan<byte> packet, CancellationToken cancellationToken)
        {
            return OnReceiveStoreRequestAsync(endpoint, header, new KPacketStoreRequest<TKNodeId>(packet), cancellationToken);
        }

        ValueTask OnReceiveStoreRequestAsync(in KIpEndpoint endpoint, in KPacketHeaderReadOnly<TKNodeId> header, in KPacketStoreRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            return OnReceiveStoreRequestAsync(endpoint, header.Sender, header.Magic, new KStoreRequest<TKNodeId>(request.Key, request.Value.ToArray()), cancellationToken);
        }

        async ValueTask OnReceiveStoreRequestAsync(KIpEndpoint endpoint, TKNodeId sender, uint magic, KStoreRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            await OnStoreReplyAsync(endpoint, sender, magic, await engine.OnStoreAsync(sender, request, cancellationToken), cancellationToken);
        }

        ValueTask OnStoreReplyAsync(in KIpEndpoint endpoint, in TKNodeId sender, uint magic, in KStoreResponse<TKNodeId> response, CancellationToken cancellationToken)
        {
            var b = new ArrayBufferWriter<byte>();
            var h = new KPacketHeader<TKNodeId>(b.GetSpan(KPacketHeaderInfo<TKNodeId>.Size));
            h.Version = 1;
            h.Sender = engine.SelfId;
            h.Magic = magic;
            h.Type = KPacketType.StoreResponse;
            KPacketStoreResponse<TKNodeId>.Write(b, response);

            var z = new byte[b.WrittenCount];
            b.WrittenSpan.CopyTo(z);
            return new ValueTask(socket.SendToAsync(new ArraySegment<byte>(z), SocketFlags.None, endpoint.ToIPEndPoint()));
        }

        /// <summary>
        /// Invoked when a FIND_NODE request is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="header"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        ValueTask OnReceiveFindNodeRequestAsync(in KIpEndpoint endpoint, in KPacketHeaderReadOnly<TKNodeId> header, ReadOnlySpan<byte> packet, CancellationToken cancellationToken)
        {
            return OnReceiveFindNodeRequestAsync(endpoint, header, new KPacketFindNodeRequest<TKNodeId>(packet), cancellationToken);
        }

        ValueTask OnReceiveFindNodeRequestAsync(in KIpEndpoint endpoint, in KPacketHeaderReadOnly<TKNodeId> header, in KPacketFindNodeRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            return OnReceiveFindNodeRequestAsync(endpoint, header.Sender, header.Magic, new KFindNodeRequest<TKNodeId>(request.Key), cancellationToken);
        }

        async ValueTask OnReceiveFindNodeRequestAsync(KIpEndpoint endpoint, TKNodeId sender, uint magic, KFindNodeRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            await OnFindNodeReplyAsync(endpoint, sender, magic, await engine.OnFindNodeAsync(sender, request, cancellationToken), cancellationToken);
        }

        ValueTask OnFindNodeReplyAsync(in KIpEndpoint endpoint, in TKNodeId sender, uint magic, in KFindNodeResponse<TKNodeId> response, CancellationToken cancellationToken)
        {
            var b = new ArrayBufferWriter<byte>();
            var h = new KPacketHeader<TKNodeId>(b.GetSpan(KPacketHeaderInfo<TKNodeId>.Size));
            h.Version = 1;
            h.Sender = engine.SelfId;
            h.Magic = magic;
            h.Type = KPacketType.FindNodeResponse;
            KPacketFindNodeResponse<TKNodeId>.Write(b, response);

            var z = new byte[b.WrittenCount];
            b.WrittenSpan.CopyTo(z);
            return new ValueTask(socket.SendToAsync(new ArraySegment<byte>(z), SocketFlags.None, endpoint.ToIPEndPoint()));
        }

        /// <summary>
        /// Invoked when a FIND_VALUE request is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="header"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        ValueTask OnReceiveFindValueRequestAsync(in KIpEndpoint endpoint, in KPacketHeaderReadOnly<TKNodeId> header, ReadOnlySpan<byte> packet, CancellationToken cancellationToken)
        {
            return OnReceiveFindValueRequestAsync(endpoint, header, new KPacketFindValueRequest<TKNodeId>(packet), cancellationToken);
        }

        ValueTask OnReceiveFindValueRequestAsync(in KIpEndpoint endpoint, in KPacketHeaderReadOnly<TKNodeId> header, in KPacketFindValueRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            return OnReceiveFindValueRequestAsync(endpoint, header.Sender, header.Magic, new KFindValueRequest<TKNodeId>(request.Key), cancellationToken);
        }

        async ValueTask OnReceiveFindValueRequestAsync(KIpEndpoint endpoint, TKNodeId sender, uint magic, KFindValueRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            await OnFindValueReplyAsync(endpoint, sender, magic, await engine.OnFindValueAsync(sender, request, cancellationToken), cancellationToken);
        }

        ValueTask OnFindValueReplyAsync(in KIpEndpoint endpoint, in TKNodeId sender, uint magic, in KFindValueResponse<TKNodeId> response, CancellationToken cancellationToken)
        {
            var b = new ArrayBufferWriter<byte>();
            var h = new KPacketHeader<TKNodeId>(b.GetSpan(KPacketHeaderInfo<TKNodeId>.Size));
            h.Version = 1;
            h.Sender = engine.SelfId;
            h.Magic = magic;
            h.Type = KPacketType.FindValueResponse;
            KPacketFindValueResponse<TKNodeId>.Write(b, response);

            var z = new byte[b.WrittenCount];
            b.WrittenSpan.CopyTo(z);
            return new ValueTask(socket.SendToAsync(new ArraySegment<byte>(z), SocketFlags.None, endpoint.ToIPEndPoint()));
        }

        /// <summary>
        /// Creates a <see cref="KResponse{TKNodeId, TResponseData}" instance.
        /// </summary>
        /// <typeparam name="TResponseData"></typeparam>
        /// <param name="sender"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        KResponse<TKNodeId, TResponseData> PackageResponse<TResponseData>(KPacketHeaderReadOnly<TKNodeId> header, in TResponseData body)
            where TResponseData : struct, IKResponseData<TKNodeId>
        {
            return new KResponse<TKNodeId, TResponseData>(header.Sender, KResponseStatus.Success, body);
        }

        /// <summary>
        /// Invoked with a PING response is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="header"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        ValueTask OnReceivePingResponseAsync(in KIpEndpoint endpoint, in KPacketHeaderReadOnly<TKNodeId> header, ReadOnlySpan<byte> packet, CancellationToken cancellationToken)
        {
            return OnReceivePingResponseAsync(endpoint, header, new KPacketPingResponse<TKNodeId>(packet), cancellationToken);
        }

        /// <summary>
        /// Invoked with a PING response is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="header"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        ValueTask OnReceivePingResponseAsync(in KIpEndpoint endpoint, in KPacketHeaderReadOnly<TKNodeId> header, in KPacketPingResponse<TKNodeId> response, CancellationToken cancellationToken)
        {
            var endpoints = new IKEndpoint<TKNodeId>[response.Endpoints.Count];
            for (var i = 0; i < endpoints.Length; i++)
                endpoints[i] = CreateEndpoint(response.Endpoints[i].Value);

            pingQueue.Respond(endpoint, header.Magic, PackageResponse(header, new KPingResponse<TKNodeId>(endpoints)));
            return new ValueTask(Task.CompletedTask);
        }

        /// <summary>
        /// Invoked with a STORE response is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="header"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        ValueTask OnReceiveStoreResponseAsync(in KIpEndpoint endpoint, in KPacketHeaderReadOnly<TKNodeId> header, ReadOnlySpan<byte> packet, CancellationToken cancellationToken)
        {
            return OnReceiveStoreResponseAsync(endpoint, header, new KPacketStoreResponse<TKNodeId>(packet), cancellationToken);
        }

        /// <summary>
        /// Invoked with a STORE response is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="header"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        ValueTask OnReceiveStoreResponseAsync(in KIpEndpoint endpoint, in KPacketHeaderReadOnly<TKNodeId> header, in KPacketStoreResponse<TKNodeId> response, CancellationToken cancellationToken)
        {
            storeQueue.Respond(endpoint, header.Magic, PackageResponse(header, new KStoreResponse<TKNodeId>(response.Key)));
            return new ValueTask(Task.CompletedTask);
        }

        /// <summary>
        /// Invoked with a FIND_NODE response is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="header"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        ValueTask OnReceiveFindNodeResponseAsync(in KIpEndpoint endpoint, in KPacketHeaderReadOnly<TKNodeId> header, ReadOnlySpan<byte> packet, CancellationToken cancellationToken)
        {
            return OnReceiveFindNodeResponseAsync(endpoint, header, new KPacketFindNodeResponse<TKNodeId>(packet), cancellationToken);
        }

        /// <summary>
        /// Invoked with a FIND_NODE response is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="header"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        ValueTask OnReceiveFindNodeResponseAsync(in KIpEndpoint endpoint, in KPacketHeaderReadOnly<TKNodeId> header, in KPacketFindNodeResponse<TKNodeId> response, CancellationToken cancellationToken)
        {
            var peers = new KPeerEndpointInfo<TKNodeId>[response.Peers.Count];
            for (var i = 0; i < peers.Length; i++)
            {
                var endpoints = new IKEndpoint<TKNodeId>[response.Peers[i].Endpoints.Count];
                for (var j = 0; i < endpoints.Length; j++)
                    endpoints[j] = CreateEndpoint(response.Peers[i].Endpoints[j].Value);

                peers[i] = new KPeerEndpointInfo<TKNodeId>(response.Peers[i].Id, endpoints);
            }

            findNodeQueue.Respond(endpoint, header.Magic, PackageResponse(header, new KFindNodeResponse<TKNodeId>(response.Key, peers)));
            return new ValueTask(Task.CompletedTask);
        }

        /// <summary>
        /// Invoked with a FIND_NODE response is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="header"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        ValueTask OnReceiveFindValueResponseAsync(in KIpEndpoint endpoint, in KPacketHeaderReadOnly<TKNodeId> header, ReadOnlySpan<byte> packet, CancellationToken cancellationToken)
        {
            return OnReceiveFindValueResponseAsync(endpoint, header, new KPacketFindValueResponse<TKNodeId>(packet), cancellationToken);
        }

        /// <summary>
        /// Invoked with a FIND_NODE response is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="header"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        ValueTask OnReceiveFindValueResponseAsync(in KIpEndpoint endpoint, in KPacketHeaderReadOnly<TKNodeId> header, in KPacketFindValueResponse<TKNodeId> response, CancellationToken cancellationToken)
        {
            var peers = new KPeerEndpointInfo<TKNodeId>[response.Peers.Count];
            for (var i = 0; i < peers.Length; i++)
            {
                var endpoints = new IKEndpoint<TKNodeId>[response.Peers[i].Endpoints.Count];
                for (var j = 0; i < endpoints.Length; j++)
                    endpoints[j] = CreateEndpoint(response.Peers[i].Endpoints[j].Value);

                peers[i] = new KPeerEndpointInfo<TKNodeId>(response.Peers[i].Id, endpoints);
            }

            findValueQueue.Respond(endpoint, header.Magic, PackageResponse(header, new KFindValueResponse<TKNodeId>(response.Key, response.ValueSize > -1 ? response.Value.ToArray() : null, peers)));
            return new ValueTask(Task.CompletedTask);
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
            using (await sync.LockAsync())
            {
                // remove any endpoints of ourselves
                foreach (var ep in endpoints.Values)
                    engine.SelfData.Endpoints.Remove(ep);

                // zero out our endpoint list
                endpoints.Clear();

                // shutdown socket
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
        }

        /// <summary>
        /// Initiates a send of the buffered data to the endpoint.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        ValueTask<int> SocketSendToAsync(ArrayBufferWriter<byte> buffer, KIpEndpoint endpoint)
        {
            var z = new byte[buffer.WrittenCount];
            buffer.WrittenSpan.CopyTo(z);
            return new ValueTask<int>(socket.SendToAsync(new ArraySegment<byte>(z), SocketFlags.None, endpoint.ToIPEndPoint()));
        }

        /// <summary>
        /// Sends the given buffer to an endpoint and begins a wait on the specified reply queue.
        /// </summary>
        /// <typeparam name="TResponseData"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="magic"></param>
        /// <param name="queue"></param>
        /// <param name="buffer"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KResponse<TKNodeId, TResponseData>> SendAndWaitAsync<TResponseData>(KIpEndpoint endpoint, uint magic, KIpResponseQueue<TKNodeId, TResponseData> queue, ArrayBufferWriter<byte> buffer, CancellationToken cancellationToken)
            where TResponseData : struct, IKResponseData<TKNodeId>
        {
            var t = queue.WaitAsync(endpoint, magic, cancellationToken);
            await SocketSendToAsync(buffer, endpoint.ToIPEndPoint());
            var r = await t;
            return r;
        }

        /// <summary>
        /// Invoked to send a PING request.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TKNodeId, KPingResponse<TKNodeId>>> PingAsync(IKEndpoint<TKNodeId> endpoint, in KPingRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            return PingAsync(endpoint, request, cancellationToken);
        }

        async ValueTask<KResponse<TKNodeId, KPingResponse<TKNodeId>>> PingAsync(IKEndpoint<TKNodeId> endpoint, KPingRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            var m = GetNextMagic();
            var b = new ArrayBufferWriter<byte>();
            KPacketHeaderReadOnly<TKNodeId>.Write(b, engine.SelfId, m, KPacketType.PingRequest);
            KPacketPingRequest<TKNodeId>.Write(b, request);
            return await SendAndWaitAsync(((KIpProtocolEndpoint<TKNodeId>)endpoint).Endpoint, m, pingQueue, b, cancellationToken);
        }

        /// <summary>
        /// Invoked to send a STORE request.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TKNodeId, KStoreResponse<TKNodeId>>> StoreAsync(IKEndpoint<TKNodeId> endpoint, in KStoreRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            return StoreAsync(endpoint, request, cancellationToken);
        }

        async ValueTask<KResponse<TKNodeId, KStoreResponse<TKNodeId>>> StoreAsync(IKEndpoint<TKNodeId> endpoint, KStoreRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            var magic = GetNextMagic();
            var buffer = new ArrayBufferWriter<byte>();
            KPacketHeaderReadOnly<TKNodeId>.Write(buffer, engine.SelfId, magic, KPacketType.StoreRequest);
            KPacketStoreRequest<TKNodeId>.Write(buffer, request);
            return await SendAndWaitAsync(((KIpProtocolEndpoint<TKNodeId>)endpoint).Endpoint, magic, storeQueue, buffer, cancellationToken);
        }

        /// <summary>
        /// Invoked to send a FIND_NODE request.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>> FindNodeAsync(IKEndpoint<TKNodeId> endpoint, in KFindNodeRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            return FindNodeAsync(endpoint, request, cancellationToken);
        }

        async ValueTask<KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>> FindNodeAsync(IKEndpoint<TKNodeId> endpoint, KFindNodeRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            var magic = GetNextMagic();
            var buffer = new ArrayBufferWriter<byte>();
            KPacketHeaderReadOnly<TKNodeId>.Write(buffer, engine.SelfId, magic, KPacketType.FindNodeRequest);
            KPacketFindNodeRequest<TKNodeId>.Write(buffer, request);
            return await SendAndWaitAsync(((KIpProtocolEndpoint<TKNodeId>)endpoint).Endpoint, magic, findNodeQueue, buffer, cancellationToken);
        }

        /// <summary>
        /// Invoked to send a FIND_VALUE request.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TKNodeId, KFindValueResponse<TKNodeId>>> FindValueAsync(IKEndpoint<TKNodeId> endpoint, in KFindValueRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            return FindValueAsync(endpoint, request, cancellationToken);
        }

        async ValueTask<KResponse<TKNodeId, KFindValueResponse<TKNodeId>>> FindValueAsync(IKEndpoint<TKNodeId> endpoint, KFindValueRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            var magic = GetNextMagic();
            var buffer = new ArrayBufferWriter<byte>();
            KPacketHeaderReadOnly<TKNodeId>.Write(buffer, engine.SelfId, magic, KPacketType.FindNodeRequest);
            KPacketFindValueRequest<TKNodeId>.Write(buffer, request);
            return await SendAndWaitAsync(((KIpProtocolEndpoint<TKNodeId>)endpoint).Endpoint, magic, findValueQueue, buffer, cancellationToken);
        }

    }

}
