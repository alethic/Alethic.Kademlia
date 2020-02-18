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
using Cogito.Kademlia.Network.Protocol.Datagram;
using Cogito.Threading;

namespace Cogito.Kademlia.Network
{

    /// <summary>
    /// Implements a simple UDP network layer.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    /// <typeparam name="TKPeerData"></typeparam>
    public class KSimpleUdpNetwork<TKNodeId, TKPeerData> : IKProtocol<TKNodeId>
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

        KIpCompletionQueue<TKNodeId, KResponse<TKNodeId, KPingResponse<TKNodeId>>> pingQueue = new KIpCompletionQueue<TKNodeId, KResponse<TKNodeId, KPingResponse<TKNodeId>>>(DEFAULT_TIMEOUT);
        KIpCompletionQueue<TKNodeId, KResponse<TKNodeId, KStoreResponse<TKNodeId>>> storeQueue = new KIpCompletionQueue<TKNodeId, KResponse<TKNodeId, KStoreResponse<TKNodeId>>>(DEFAULT_TIMEOUT);
        KIpCompletionQueue<TKNodeId, KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>> findNodeQueue = new KIpCompletionQueue<TKNodeId, KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>>(DEFAULT_TIMEOUT);
        KIpCompletionQueue<TKNodeId, KResponse<TKNodeId, KFindValueResponse<TKNodeId>>> findValueQueue = new KIpCompletionQueue<TKNodeId, KResponse<TKNodeId, KFindValueResponse<TKNodeId>>>(DEFAULT_TIMEOUT);

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
        /// Initiates a connection to the specified endpoint.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask ConnectAsync(in KIpEndpoint endpoint, CancellationToken cancellationToken = default)
        {
            return ConnectAsync(endpoint, cancellationToken);
        }

        /// <summary>
        /// Initiates a connection to the specified endpoint.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask ConnectAsync(KIpEndpoint endpoint, CancellationToken cancellationToken = default)
        {
            // initiate anonymous ping to endpoint and update peer with ping results
            var r = await PingAsync(endpoint, new KPingRequest<TKNodeId>(Endpoints.ToArray()), cancellationToken);
            await engine.Router.UpdatePeerAsync(r.Sender, r.Body.Endpoints.ToArray(), cancellationToken);
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
                        yield return new KIpEndpoint(new KIp4Endpoint(new KIp4Address(i), port != 0 ? port : ((IPEndPoint)socket.LocalEndPoint).Port));
                        break;
                    case AddressFamily.InterNetworkV6:
                        yield return new KIpEndpoint(new KIp6Endpoint(new KIp6Address(i), port != 0 ? port : ((IPEndPoint)socket.LocalEndPoint).Port));
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
                    endpoints[ip] = new KIpProtocolEndpoint<TKNodeId>(this, engine.SelfId, ip);

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
            Task.Run(async () => { try { var s = new ReadOnlySequence<byte>(m); await OnReceiveAsync(p, ref s); } catch { } finally { o.Dispose(); } });

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
        ValueTask OnReceiveAsync(in KIpEndpoint endpoint, ref ReadOnlySequence<byte> packet)
        {
            // check for continued connection
            var s = socket;
            if (s == null || s.IsBound == false)
                return new ValueTask(Task.CompletedTask);

            var header = KPacketReader<TKNodeId>.ReadHeader(ref packet);
            return header.Type switch
            {
                KPacketType.PingRequest => OnReceivePingRequestAsync(endpoint, header, ref packet),
                KPacketType.PingResponse => OnReceivePingResponseAsync(endpoint, header, ref packet),
                KPacketType.StoreRequest => OnReceiveStoreRequestAsync(endpoint, header, ref packet),
                KPacketType.StoreResponse => OnReceiveStoreResponseAsync(endpoint, header, ref packet),
                KPacketType.FindNodeRequest => OnReceiveFindNodeRequestAsync(endpoint, header, ref packet),
                KPacketType.FindNodeResponse => OnReceiveFindNodeResponseAsync(endpoint, header, ref packet),
                KPacketType.FindValueRequest => OnReceiveFindValueRequestAsync(endpoint, header, ref packet),
                KPacketType.FindValueResponse => OnReceiveFindValueResponseAsync(endpoint, header, ref packet),
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
        ValueTask OnReceivePingRequestAsync(in KIpEndpoint endpoint, in KPacketHeader<TKNodeId> header, ref ReadOnlySequence<byte> packet)
        {
            return OnReceivePingRequestAsync(endpoint, header, KPacketReader<TKNodeId>.ReadPingRequest(ref packet));
        }

        /// <summary>
        /// Invoked when a PING request is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="header"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        ValueTask OnReceivePingRequestAsync(in KIpEndpoint endpoint, in KPacketHeader<TKNodeId> header, in KPingRequestBody<TKNodeId> request)
        {
            return OnReceivePingRequestAsync(endpoint, header.Sender, header.Magic);
        }

        /// <summary>
        /// Invoked when a PING request is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="sender"></param>
        /// <param name="magic"></param>
        /// <returns></returns>
        async ValueTask OnReceivePingRequestAsync(KIpEndpoint endpoint, TKNodeId sender, uint magic)
        {
            var r = await engine.OnPingAsync(sender, new KPingRequest<TKNodeId>(), CancellationToken.None);
            var b = new ArrayBufferWriter<byte>();
            KPacketWriter<TKNodeId>.WriteHeader(b, new KPacketHeader<TKNodeId>(engine.SelfId, magic, KPacketType.PingResponse));
            KPacketWriter<TKNodeId>.WritePingResponse(b, new KPingResponseBody<TKNodeId>(r.Endpoints.ToArray().OfType<KIpProtocolEndpoint<TKNodeId>>().Select(i => i.Endpoint).ToArray()));
            var z = new byte[b.WrittenCount];
            b.WrittenSpan.CopyTo(z);
            await socket.SendToAsync(new ArraySegment<byte>(z), SocketFlags.None, endpoint.ToIPEndPoint());
        }

        /// <summary>
        /// Invoked when a STORE request is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="header"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        ValueTask OnReceiveStoreRequestAsync(in KIpEndpoint endpoint, in KPacketHeader<TKNodeId> header, ref ReadOnlySequence<byte> packet)
        {
            return OnReceiveStoreRequestAsync(endpoint, header, KPacketReader<TKNodeId>.ReadStoreRequest(ref packet));
        }

        /// <summary>
        /// Invoked when a STORE request is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="header"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        ValueTask OnReceiveStoreRequestAsync(in KIpEndpoint endpoint, in KPacketHeader<TKNodeId> header, in KStoreRequestBody<TKNodeId> request)
        {
            return OnReceiveStoreRequestAsync(endpoint, header.Sender, header.Magic, request.Key, request.Value.ToArray());
        }

        /// <summary>
        /// Invoked when a STORE request is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="sender"></param>
        /// <param name="magic"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        async ValueTask OnReceiveStoreRequestAsync(KIpEndpoint endpoint, TKNodeId sender, uint magic, TKNodeId key, ReadOnlyMemory<byte> value)
        {
            var r = await engine.OnStoreAsync(sender, new KStoreRequest<TKNodeId>(key, value), CancellationToken.None);
            var b = new ArrayBufferWriter<byte>();
            KPacketWriter<TKNodeId>.WriteHeader(b, new KPacketHeader<TKNodeId>(engine.SelfId, magic, KPacketType.StoreResponse));
            KPacketWriter<TKNodeId>.WriteStoreResponse(b, new KStoreResponseBody<TKNodeId>(key));
            var z = new byte[b.WrittenCount];
            b.WrittenSpan.CopyTo(z);
            await socket.SendToAsync(new ArraySegment<byte>(z), SocketFlags.None, endpoint.ToIPEndPoint());
        }

        /// <summary>
        /// Invoked when a FIND_NODE request is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="header"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        ValueTask OnReceiveFindNodeRequestAsync(in KIpEndpoint endpoint, in KPacketHeader<TKNodeId> header, ref ReadOnlySequence<byte> packet)
        {
            return OnReceiveFindNodeRequestAsync(endpoint, header, KPacketReader<TKNodeId>.ReadFindNodeRequest(ref packet));
        }

        /// <summary>
        /// Invoked when a FIND_NODE request is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="header"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        ValueTask OnReceiveFindNodeRequestAsync(in KIpEndpoint endpoint, in KPacketHeader<TKNodeId> header, in KFindNodeRequestBody<TKNodeId> request)
        {
            return OnReceiveFindNodeRequestAsync(endpoint, header.Sender, header.Magic, request.NodeId);
        }

        /// <summary>
        /// Invoked when a FIND_NODE request is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="sender"></param>
        /// <param name="magic"></param>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        async ValueTask OnReceiveFindNodeRequestAsync(KIpEndpoint endpoint, TKNodeId sender, uint magic, TKNodeId nodeId)
        {
            var r = await engine.OnFindNodeAsync(sender, new KFindNodeRequest<TKNodeId>(nodeId), CancellationToken.None);
            var b = new ArrayBufferWriter<byte>();
            KPacketWriter<TKNodeId>.WriteHeader(b, new KPacketHeader<TKNodeId>(engine.SelfId, magic, KPacketType.StoreResponse));
            KPacketWriter<TKNodeId>.WriteFindNodeResponse(b, new KFindNodeResponseBody<TKNodeId>(nodeId));
            var z = new byte[b.WrittenCount];
            b.WrittenSpan.CopyTo(z);
            await socket.SendToAsync(new ArraySegment<byte>(z), SocketFlags.None, endpoint.ToIPEndPoint());
        }

        /// <summary>
        /// Invoked when a FIND_VALUE request is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="header"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        ValueTask OnReceiveFindValueRequestAsync(in KIpEndpoint endpoint, in KPacketHeader<TKNodeId> header, ref ReadOnlySequence<byte> packet)
        {
            return OnReceiveFindValueRequestAsync(endpoint, header, KPacketReader<TKNodeId>.ReadFindValueRequest(ref packet));
        }

        /// <summary>
        /// Invoked when a FIND_VALUE request is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="header"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        ValueTask OnReceiveFindValueRequestAsync(in KIpEndpoint endpoint, in KPacketHeader<TKNodeId> header, in KFindValueRequestBody<TKNodeId> request)
        {
            return OnReceiveFindValueRequestAsync(endpoint, header.Sender, header.Magic, request.Key);
        }

        /// <summary>
        /// Invoked when a FIND_VALUE request is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="sender"></param>
        /// <param name="magic"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        async ValueTask OnReceiveFindValueRequestAsync(KIpEndpoint endpoint, TKNodeId sender, uint magic, TKNodeId key)
        {
            var r = await engine.OnFindValueAsync(sender, new KFindValueRequest<TKNodeId>(key), CancellationToken.None);
            var b = new ArrayBufferWriter<byte>();
            KPacketWriter<TKNodeId>.WriteHeader(b, new KPacketHeader<TKNodeId>(engine.SelfId, magic, KPacketType.StoreResponse));
            KPacketWriter<TKNodeId>.WriteFindValueResponse(b, new KFindValueResponseBody<TKNodeId>(key, r.Value.Span));
            var z = new byte[b.WrittenCount];
            b.WrittenSpan.CopyTo(z);
            await socket.SendToAsync(new ArraySegment<byte>(z), SocketFlags.None, endpoint.ToIPEndPoint());
        }

        /// <summary>
        /// Creates a <see cref="KResponse{TKNodeId, TKResponseBody}" instance.
        /// </summary>
        /// <typeparam name="TResponseType"></typeparam>
        /// <param name="sender"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        KResponse<TKNodeId, TResponseType> PackageResponse<TResponseType>(in KPacketHeader<TKNodeId> header, in TResponseType body)
        {
            return new KResponse<TKNodeId, TResponseType>(header.Sender, body);
        }

        /// <summary>
        /// Invoked with a PING response is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="header"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        ValueTask OnReceivePingResponseAsync(in KIpEndpoint endpoint, in KPacketHeader<TKNodeId> header, ref ReadOnlySequence<byte> packet)
        {
            return OnReceivePingResponseAsync(endpoint, header, KPacketReader<TKNodeId>.ReadPingResponse(ref packet));
        }

        /// <summary>
        /// Invoked with a PING response is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="header"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        ValueTask OnReceivePingResponseAsync(in KIpEndpoint endpoint, in KPacketHeader<TKNodeId> header, in KPingResponseBody<TKNodeId> response)
        {
            var endpoints = new IKEndpoint<TKNodeId>[response.Endpoints.Length];
            for (var i = 0; i < endpoints.Length; i++)
                endpoints[i] = new KIpProtocolEndpoint<TKNodeId>(this, header.Sender, response.Endpoints[i]);

            pingQueue.Release(endpoint, header.Magic, PackageResponse(header, new KPingResponse<TKNodeId>(endpoints)));
            return new ValueTask(Task.CompletedTask);
        }

        /// <summary>
        /// Invoked with a STORE response is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="header"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        ValueTask OnReceiveStoreResponseAsync(in KIpEndpoint endpoint, in KPacketHeader<TKNodeId> header, ref ReadOnlySequence<byte> packet)
        {
            return OnReceiveStoreResponseAsync(endpoint, header, KPacketReader<TKNodeId>.ReadStoreResponse(ref packet));
        }

        /// <summary>
        /// Invoked with a STORE response is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="header"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        ValueTask OnReceiveStoreResponseAsync(in KIpEndpoint endpoint, in KPacketHeader<TKNodeId> header, in KStoreResponseBody<TKNodeId> response)
        {
            storeQueue.Release(endpoint, header.Magic, PackageResponse(header, new KStoreResponse<TKNodeId>(response.Key)));
            return new ValueTask(Task.CompletedTask);
        }

        /// <summary>
        /// Invoked with a FIND_NODE response is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="header"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        ValueTask OnReceiveFindNodeResponseAsync(in KIpEndpoint endpoint, in KPacketHeader<TKNodeId> header, ref ReadOnlySequence<byte> packet)
        {
            return OnReceiveFindNodeResponseAsync(endpoint, header, KPacketReader<TKNodeId>.ReadFindNodeResponse(ref packet));
        }

        /// <summary>
        /// Invoked with a FIND_NODE response is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="header"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        ValueTask OnReceiveFindNodeResponseAsync(in KIpEndpoint endpoint, in KPacketHeader<TKNodeId> header, in KFindNodeResponseBody<TKNodeId> response)
        {
            findNodeQueue.Release(endpoint, header.Magic, PackageResponse(header, new KFindNodeResponse<TKNodeId>(response.NodeId)));
            return new ValueTask(Task.CompletedTask);
        }

        /// <summary>
        /// Invoked with a FIND_NODE response is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="header"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        ValueTask OnReceiveFindValueResponseAsync(in KIpEndpoint endpoint, in KPacketHeader<TKNodeId> header, ref ReadOnlySequence<byte> packet)
        {
            return OnReceiveFindValueResponseAsync(endpoint, header, KPacketReader<TKNodeId>.ReadFindValueResponse(ref packet));
        }

        /// <summary>
        /// Invoked with a FIND_NODE response is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="header"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        ValueTask OnReceiveFindValueResponseAsync(in KIpEndpoint endpoint, in KPacketHeader<TKNodeId> header, in KFindValueResponseBody<TKNodeId> response)
        {
            findValueQueue.Release(endpoint, header.Magic, PackageResponse(header, new KFindValueResponse<TKNodeId>(response.Key, response.Value.ToArray())));
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
        /// Invoked to send a PING request.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KResponse<TKNodeId, KPingResponse<TKNodeId>>> IKProtocol<TKNodeId>.PingAsync(in IKEndpoint<TKNodeId> endpoint, in KPingRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            if (endpoint is KIpProtocolEndpoint<TKNodeId> ep)
                return PingAsync(ep.Endpoint, request, cancellationToken);

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Invoked to send a PING request.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KResponse<TKNodeId, KPingResponse<TKNodeId>>> PingAsync(KIpEndpoint endpoint, KPingRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            var m = GetNextMagic();
            var b = new ArrayBufferWriter<byte>();
            KPacketWriter<TKNodeId>.WriteHeader(b, new KPacketHeader<TKNodeId>(engine.SelfId, m, KPacketType.PingRequest));
            KPacketWriter<TKNodeId>.WritePingRequest(b, new KPingRequestBody<TKNodeId>(request.Endpoints.ToArray().OfType<KIpProtocolEndpoint<TKNodeId>>().Where(i => i.Protocol == this).Select(i => i.Endpoint).ToArray()));
            var z = new byte[b.WrittenCount];
            b.WrittenSpan.CopyTo(z);
            var t = pingQueue.Enqueue(endpoint, m, cancellationToken);
            var asdasd = endpoint.ToIPEndPoint();
            await socket.SendToAsync(new ArraySegment<byte>(z), SocketFlags.None, endpoint.ToIPEndPoint());
            var r = await t;
            return r;
        }

        /// <summary>
        /// Invoked to send a STORE request.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KResponse<TKNodeId, KStoreResponse<TKNodeId>>> IKProtocol<TKNodeId>.StoreAsync(in IKEndpoint<TKNodeId> endpoint, in KStoreRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            if (endpoint is KIpProtocolEndpoint<TKNodeId> ep)
                return StoreAsync(ep.Endpoint, request, cancellationToken);

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Invoked to send a STORE request.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KResponse<TKNodeId, KStoreResponse<TKNodeId>>> StoreAsync(KIpEndpoint endpoint, KStoreRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            var m = GetNextMagic();
            var b = new ArrayBufferWriter<byte>();
            KPacketWriter<TKNodeId>.WriteHeader(b, new KPacketHeader<TKNodeId>(engine.SelfId, m, KPacketType.StoreRequest));
            KPacketWriter<TKNodeId>.WriteStoreRequest(b, new KStoreRequestBody<TKNodeId>(request.Key, request.Value.Span));
            var z = new byte[b.WrittenCount];
            b.WrittenSpan.CopyTo(z);
            var t = storeQueue.Enqueue(endpoint, m, cancellationToken);
            await socket.SendToAsync(new ArraySegment<byte>(z), SocketFlags.None, endpoint.ToIPEndPoint());
            var r = await t;
            return r;
        }

        /// <summary>
        /// Invoked to send a FIND_NODE request.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>> IKProtocol<TKNodeId>.FindNodeAsync(in IKEndpoint<TKNodeId> endpoint, in KFindNodeRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            if (endpoint is KIpProtocolEndpoint<TKNodeId> ep)
                return FindNodeAsync(ep.Endpoint, request, cancellationToken);

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Invoked to send a STORE request.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>> FindNodeAsync(KIpEndpoint endpoint, KFindNodeRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            var m = GetNextMagic();
            var b = new ArrayBufferWriter<byte>();
            KPacketWriter<TKNodeId>.WriteHeader(b, new KPacketHeader<TKNodeId>(engine.SelfId, m, KPacketType.FindNodeRequest));
            KPacketWriter<TKNodeId>.WriteFindNodeRequest(b, new KFindNodeRequestBody<TKNodeId>(request.NodeId));
            var z = new byte[b.WrittenCount];
            b.WrittenSpan.CopyTo(z);
            var t = findNodeQueue.Enqueue(endpoint, m, cancellationToken);
            await socket.SendToAsync(new ArraySegment<byte>(z), SocketFlags.None, endpoint.ToIPEndPoint());
            var r = await t;
            return r;
        }

        /// <summary>
        /// Invoked to send a FIND_VALUE request.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KResponse<TKNodeId, KFindValueResponse<TKNodeId>>> IKProtocol<TKNodeId>.FindValueAsync(in IKEndpoint<TKNodeId> endpoint, in KFindValueRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            if (endpoint is KIpProtocolEndpoint<TKNodeId> ep)
                return FindNodeAsync(ep.Endpoint, request, cancellationToken);

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Invoked to send a STORE request.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KResponse<TKNodeId, KFindValueResponse<TKNodeId>>> FindNodeAsync(KIpEndpoint endpoint, KFindValueRequest<TKNodeId> request, CancellationToken cancellationToken)
        {
            var m = GetNextMagic();
            var b = new ArrayBufferWriter<byte>();
            KPacketWriter<TKNodeId>.WriteHeader(b, new KPacketHeader<TKNodeId>(engine.SelfId, m, KPacketType.FindValueRequest));
            KPacketWriter<TKNodeId>.WriteFindValueRequest(b, new KFindValueRequestBody<TKNodeId>(request.Key));
            var z = new byte[b.WrittenCount];
            b.WrittenSpan.CopyTo(z);
            var t = findValueQueue.Enqueue(endpoint, m, cancellationToken);
            await socket.SendToAsync(new ArraySegment<byte>(z), SocketFlags.None, endpoint.ToIPEndPoint());
            var r = await t;
            return r;
        }

    }

}
