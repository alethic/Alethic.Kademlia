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
using Cogito.Kademlia.Network;
using Cogito.Threading;

using Microsoft.Extensions.Logging;

namespace Cogito.Kademlia.Protocols.Udp
{

    /// <summary>
    /// Implements a simple UDP network layer.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    /// <typeparam name="TKPeerData"></typeparam>
    public class KUdpProtocol<TKNodeId, TKPeerData> : IKIpProtocol<TKNodeId>, IKIpProtocolResourceProvider<TKNodeId>
        where TKNodeId : unmanaged
        where TKPeerData : IKEndpointProvider<TKNodeId>
    {

        static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);
        static readonly Random rnd = new Random();

        readonly ulong network;
        readonly IKEngine<TKNodeId, TKPeerData> engine;
        readonly IKMessageDecoder<TKNodeId, IKIpProtocolResourceProvider<TKNodeId>> decoder;
        readonly IKMessageEncoder<TKNodeId, IKIpProtocolResourceProvider<TKNodeId>> encoder;
        readonly KIpEndpoint listen;
        readonly ILogger logger;
        readonly AsyncLock sync = new AsyncLock();
        readonly Dictionary<KIpEndpoint, KIpProtocolEndpoint<TKNodeId>> endpoints = new Dictionary<KIpEndpoint, KIpProtocolEndpoint<TKNodeId>>();

        readonly KResponseQueue<TKNodeId, KPingResponse<TKNodeId>, ulong> pingQueue = new KResponseQueue<TKNodeId, KPingResponse<TKNodeId>, ulong>(DefaultTimeout);
        readonly KResponseQueue<TKNodeId, KStoreResponse<TKNodeId>, ulong> storeQueue = new KResponseQueue<TKNodeId, KStoreResponse<TKNodeId>, ulong>(DefaultTimeout);
        readonly KResponseQueue<TKNodeId, KFindNodeResponse<TKNodeId>, ulong> findNodeQueue = new KResponseQueue<TKNodeId, KFindNodeResponse<TKNodeId>, ulong>(DefaultTimeout);
        readonly KResponseQueue<TKNodeId, KFindValueResponse<TKNodeId>, ulong> findValueQueue = new KResponseQueue<TKNodeId, KFindValueResponse<TKNodeId>, ulong>(DefaultTimeout);

        Socket sendSocket;
        Dictionary<IPAddress, Socket> recvSockets;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="network"></param>
        /// <param name="engine"></param>
        /// <param name="encoder"></param>
        /// <param name="decoder"></param>
        /// <param name="listen"></param>
        /// <param name="logger"></param>
        public KUdpProtocol(ulong network, IKEngine<TKNodeId, TKPeerData> engine, IKMessageEncoder<TKNodeId, IKIpProtocolResourceProvider<TKNodeId>> encoder, IKMessageDecoder<TKNodeId, IKIpProtocolResourceProvider<TKNodeId>> decoder, KIpEndpoint? listen = null, ILogger logger = null)
        {
            this.network = network;
            this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
            this.encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
            this.decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
            this.listen = listen ?? KIpEndpoint.AnyV6;
            this.logger = logger;
        }

        /// <summary>
        /// Uniquely idenfies the traffic for this network from others.
        /// </summary>
        public ulong Network => network;

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
        /// Creates a new <see cref="KIpProtocolEndpoint{TKNodeId}"/> for the given <see cref="KIpEndpoint"/>.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        IKEndpoint<TKNodeId> IKIpProtocolResourceProvider<TKNodeId>.CreateEndpoint(in KIpEndpoint endpoint)
        {
            return CreateEndpoint(endpoint);
        }

        /// <summary>
        /// Gets the next magic value.
        /// </summary>
        /// <returns></returns>
        ulong NewMagic()
        {
            return (ulong)rnd.NextInt64();
        }

        /// <summary>
        /// Gets the local available IP addresses.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IPAddress> GetLocalIpAddresses()
        {
            var h = new HashSet<IPAddress>();
            if (NetworkInterface.GetIsNetworkAvailable())
                foreach (var i in NetworkInterface.GetAllNetworkInterfaces())
                    if (i.OperationalStatus == OperationalStatus.Up)
                        foreach (var j in i.GetIPProperties().UnicastAddresses)
                            if (j.Address.IsIPv4MappedToIPv6 == false && j.Address.IsIPv6Multicast == false && j.Address.IsIPv6SiteLocal == false)
                                if (h.Add(j.Address))
                                    yield return j.Address;
        }

        /// <summary>
        /// Scans for new IP addresses and creates receive sockets.
        /// </summary>
        void RefreshReceiveSockets()
        {
            // determine port, either we already have one allocated previously, or we need to generate a new one
            var recvPort = (ushort?)recvSockets.Select(i => i.Value.LocalEndPoint).Cast<IPEndPoint>().FirstOrDefault()?.Port ?? listen.Port;
            var ipListen = GetLocalIpAddresses();

            // the IPs we listen to are governed by the 'listen' endpoint
            switch (listen.Protocol)
            {
                case KIpAddressFamily.IPv4:
                    {
                        // listen only on V4
                        ipListen = ipListen.Where(i => i.AddressFamily == AddressFamily.InterNetwork);

                        // listen only on specific address
                        if (listen.V4 != KIp4Address.Any)
                            ipListen = ipListen.Where(i => new KIp4Address(i) == listen.V4);
                        break;
                    }

                case KIpAddressFamily.IPv6:
                    {
                        // listen only on V6
                        ipListen = ipListen.Where(i => i.AddressFamily == AddressFamily.InterNetworkV6);

                        // listen only on specific address
                        if (listen.V6 != KIp6Address.Any)
                            ipListen = ipListen.Where(i => new KIp6Address(i) == listen.V6);
                        break;
                    }
                default:
                    // no listen protocol specified, so allow everything
                    break;
            }

            // set of sockets to keep
            var keepSockets = new List<Socket>();

            // generate one socket per IP
            foreach (var ip in ipListen)
            {
                var socketOptionLevelIp = ip.AddressFamily switch
                {
                    AddressFamily.InterNetwork => SocketOptionLevel.IP,
                    AddressFamily.InterNetworkV6 => SocketOptionLevel.IPv6,
                    _ => throw new InvalidOperationException(),
                };

                // skip already generated sockets
                if (recvSockets.TryGetValue(ip, out var recvSocket))
                {
                    keepSockets.Add(recvSocket);
                    continue;
                }

                // establish UDP socket
                recvSocket = new Socket(ip.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                recvSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, recvPort == 0);
                recvSocket.Bind(new IPEndPoint(ip, recvPort));
                recvSockets[ip] = recvSocket;
                keepSockets.Add(recvSocket);

                // record relation between endpoint data and endpoint interface
                var ep = new KIpEndpoint((IPEndPoint)recvSocket.LocalEndPoint);
                engine.SelfData.Endpoints.Demote(endpoints[ep] = CreateEndpoint(ep));
                logger?.LogInformation("Initialized receiving UDP socket on {Endpoint}.", ep);

                // following sockets will preserve port
                recvPort = ep.Port;

                // begin receiving
                var recvArgs = new SocketAsyncEventArgs();
                recvArgs.Completed += SocketAsyncEventArgs_Completed;
                BeginReceive(recvSocket, recvArgs);
            }

            // dispose of sockets not marked off in keep
            foreach (var i in recvSockets.Where(i => keepSockets.Contains(i.Value) == false).ToList())
            {
                logger?.LogInformation("Disposing UDP socket for {Endpoint}.", i.Key);
                recvSockets.Remove(i.Key);

                // shutdown the socket as best we can
                try
                {
                    i.Value.Close();
                    i.Value.Dispose();
                }
                finally
                {

                }

                // find existing advertised endpoint and remove
                var l = endpoints.FirstOrDefault(j => j.Key.ToIPEndPoint().Address == i.Key);
                if (l.Value != null)
                {
                    endpoints.Remove(l.Key);
                    engine.SelfData.Endpoints.Remove(l.Value);
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
            using (await sync.LockAsync(cancellationToken))
            {
                if (sendSocket != null || recvSockets != null)
                    throw new KException("UDP protocol is already started.");

                // reset sockets
                sendSocket = null;
                recvSockets = new Dictionary<IPAddress, Socket>();

                // remove our previous advertised endpoints; there should be none
                foreach (var i in engine.SelfData.Endpoints.Where(i => i.Protocol == this).ToList())
                    engine.SelfData.Endpoints.Remove(i);

                // listen protocol determines send socket binding
                switch (listen.Protocol)
                {
                    case KIpAddressFamily.Unknown:
                        // establish UDP socket for both IPv4 and IPv6 (dual mode)
                        sendSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                        sendSocket.DualMode = true;
                        sendSocket.Bind(new IPEndPoint(IPAddress.IPv6Any, 0));
                        logger?.LogInformation("Initialized sending UDP socket on {Endpoint}.", sendSocket.LocalEndPoint);
                        break;
                    case KIpAddressFamily.IPv4:
                        // establish UDP socket for IPv4
                        sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                        sendSocket.Bind(new IPEndPoint(IPAddress.Any, 0));
                        logger?.LogInformation("Initialized sending UDP socket on {Endpoint}.", sendSocket.LocalEndPoint);
                        break;
                    case KIpAddressFamily.IPv6:
                        // establish UDP socket for IPv6
                        sendSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                        sendSocket.Bind(new IPEndPoint(IPAddress.IPv6Any, 0));
                        logger?.LogInformation("Initialized sending UDP socket on {Endpoint}.", sendSocket.LocalEndPoint);
                        break;
                }

                // begin receiving from send socket
                var sendArgs = new SocketAsyncEventArgs();
                sendArgs.Completed += SocketAsyncEventArgs_Completed;
                BeginReceive(sendSocket, sendArgs);

                // configure receive sockets and update on IP address change
                NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
                RefreshReceiveSockets();
            }
        }

        /// <summary>
        /// Invoked when a local network address changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        async void NetworkChange_NetworkAddressChanged(object sender, EventArgs args)
        {
            using (await sync.LockAsync())
                RefreshReceiveSockets();
        }

        /// <summary>
        /// Initiates a receive for the specified socket.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="args"></param>
        void BeginReceive(Socket socket, SocketAsyncEventArgs args)
        {
            // allocate new buffer to receive into
            var buff = ArrayPool<byte>.Shared.Rent(8192);

            // reconfigure event args
            args.SetBuffer(buff, 0, buff.Length);
            args.RemoteEndPoint = new IPEndPoint(socket.AddressFamily switch
            {
                AddressFamily.InterNetwork => IPAddress.Any,
                AddressFamily.InterNetworkV6 => IPAddress.IPv6Any,
                _ => throw new InvalidOperationException(),
            }, 0);

            // queue wait for packet
            if (socket.ReceiveFromAsync(args) == false)
                SocketAsyncEventArgs_Completed(socket, args);
        }

        /// <summary>
        /// Invoked when a receive operation completes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void SocketAsyncEventArgs_Completed(object sender, SocketAsyncEventArgs args)
        {
            // some error occurred?
            if (args.SocketError != SocketError.Success)
                return;

            // we only care about receive from events
            if (args.LastOperation != SocketAsyncOperation.ReceiveFrom)
                return;

            try
            {
                // check state of current socket
                var socket = (Socket)sender;
                if (socket.IsBound == false)
                    return;

                if (args.BytesTransferred > 0)
                {
                    logger?.LogInformation("Received incoming packet of {Size} from {Endpoint}.", args.BytesTransferred, (IPEndPoint)args.RemoteEndPoint);

                    // obtain information about packet
                    var endpoint = new KIpEndpoint((IPEndPoint)args.RemoteEndPoint);

                    // extract memory lease and slice to received size
                    var b = args.Buffer;
                    var m = b.AsMemory().Slice(0, args.BytesTransferred);

                    // schedule receive on task pool
                    Task.Run(async () =>
                    {
                        try
                        {
                            await OnReceiveAsync(socket, endpoint, m.Span, CancellationToken.None);
                        }
                        catch (Exception e)
                        {
                            logger?.LogError(e, "Unhandled exception dispatching incoming packet.");
                        }
                        finally
                        {
                            // return the buffer to the pool
                            ArrayPool<byte>.Shared.Return(b);
                        }
                    });
                }

                // wait for next packet
                if (socket.IsBound)
                    BeginReceive(socket, args);
            }
            catch (SocketException e)
            {
                logger?.LogError(e, "Exception during UDP receive.");
            }
        }

        /// <summary>
        /// Invoked when a datagram is received.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="endpoint"></param>
        /// <param name="packet"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask OnReceiveAsync(Socket socket, in KIpEndpoint endpoint, ReadOnlySpan<byte> packet, CancellationToken cancellationToken)
        {
            // decode incoming byte sequence
            var sequence = decoder.Decode(this, new ReadOnlySequence<byte>(packet.ToArray()));
            if (sequence.Network != network)
            {
                logger?.LogWarning("Received unexpected message sequence for network {NetworkId}.", sequence.Network);
                return new ValueTask(Task.CompletedTask);
            }

            var todo = new List<Task>();

            // dispatch individual messages into infrastructure
            foreach (var message in sequence)
            {
                todo.Add(message switch
                {
                    KMessage<TKNodeId, KPingRequest<TKNodeId>> r => OnReceivePingRequestAsync(socket, endpoint, r, cancellationToken).AsTask(),
                    KMessage<TKNodeId, KPingResponse<TKNodeId>> r => OnReceivePingResponseAsync(socket, endpoint, r, cancellationToken).AsTask(),
                    KMessage<TKNodeId, KStoreRequest<TKNodeId>> r => OnReceiveStoreRequestAsync(socket, endpoint, r, cancellationToken).AsTask(),
                    KMessage<TKNodeId, KStoreResponse<TKNodeId>> r => OnReceiveStoreResponseAsync(socket, endpoint, r, cancellationToken).AsTask(),
                    KMessage<TKNodeId, KFindNodeRequest<TKNodeId>> r => OnReceiveFindNodeRequestAsync(socket, endpoint, r, cancellationToken).AsTask(),
                    KMessage<TKNodeId, KFindNodeResponse<TKNodeId>> r => OnReceiveFindNodeResponseAsync(socket, endpoint, r, cancellationToken).AsTask(),
                    KMessage<TKNodeId, KFindValueRequest<TKNodeId>> r => OnReceiveFindValueRequestAsync(socket, endpoint, r, cancellationToken).AsTask(),
                    KMessage<TKNodeId, KFindValueResponse<TKNodeId>> r => OnReceiveFindValueResponseAsync(socket, endpoint, r, cancellationToken).AsTask(),
                    _ => Task.CompletedTask,
                });
            }

            // return when all complete
            return new ValueTask(Task.WhenAll(todo));
        }

        /// <summary>
        /// Packages up a new message originating from this host.
        /// </summary>
        /// <typeparam name="TBody"></typeparam>
        /// <param name="magic"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        KMessageSequence<TKNodeId> PackageMessage<TBody>(ulong magic, TBody body)
            where TBody : struct, IKMessageBody<TKNodeId>
        {
            return new KMessageSequence<TKNodeId>(network, new[] { (IKMessage<TKNodeId>)new KMessage<TKNodeId, TBody>(new KMessageHeader<TKNodeId>(engine.SelfId, magic), body) });
        }

        /// <summary>
        /// Invoked when a PING request is received.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask OnReceivePingRequestAsync(Socket socket, KIpEndpoint endpoint, KMessage<TKNodeId, KPingRequest<TKNodeId>> request, CancellationToken cancellationToken)
        {
            logger?.LogDebug("Received {Operation}:{Magic} from {Sender} at {Endpoint}.", "PING", request.Header.Magic, request.Header.Sender, endpoint);
            await OnPingReplyAsync(socket, endpoint, request.Header.Magic, await engine.OnPingAsync(request.Header.Sender, CreateEndpoint(endpoint), request.Body, cancellationToken), cancellationToken);
        }

        ValueTask OnPingReplyAsync(Socket socket, in KIpEndpoint endpoint, ulong magic, in KPingResponse<TKNodeId> response, CancellationToken cancellationToken)
        {
            logger?.LogDebug("Sending {Operation}:{Magic} reply to {Endpoint}.", "PING", magic, endpoint);
            var b = new ArrayBufferWriter<byte>();
            encoder.Encode(this, b, PackageMessage(magic, response));
            return SocketSendToAsync(socket, b, endpoint, cancellationToken);
        }

        /// <summary>
        /// Invoked when a STORE request is received.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask OnReceiveStoreRequestAsync(Socket socket, KIpEndpoint endpoint, KMessage<TKNodeId, KStoreRequest<TKNodeId>> request, CancellationToken cancellationToken)
        {
            logger?.LogDebug("Received {Operation}:{Magic} from {Sender} at {Endpoint}.", "STORE", request.Header.Magic, request.Header.Sender, endpoint);
            await OnStoreReplyAsync(socket, endpoint, request.Header.Magic, await engine.OnStoreAsync(request.Header.Sender, CreateEndpoint(endpoint), request.Body, cancellationToken), cancellationToken);
        }

        ValueTask OnStoreReplyAsync(Socket socket, in KIpEndpoint endpoint, ulong magic, in KStoreResponse<TKNodeId> response, CancellationToken cancellationToken)
        {
            logger?.LogDebug("Sending {Operation}:{Magic} reply to {Endpoint}.", "STORE", magic, endpoint);
            var b = new ArrayBufferWriter<byte>();
            encoder.Encode(this, b, PackageMessage(magic, response));
            return SocketSendToAsync(socket, b, endpoint, cancellationToken);
        }

        /// <summary>
        /// Invoked when a FIND_NODE request is received.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask OnReceiveFindNodeRequestAsync(Socket socket, KIpEndpoint endpoint, KMessage<TKNodeId, KFindNodeRequest<TKNodeId>> request, CancellationToken cancellationToken)
        {
            logger?.LogDebug("Received {Operation}:{Magic} from {Sender} at {Endpoint}.", "FIND_NODE", request.Header.Magic, request.Header.Sender, endpoint);
            await OnFindNodeReplyAsync(socket, endpoint, request.Header.Magic, await engine.OnFindNodeAsync(request.Header.Sender, CreateEndpoint(endpoint), request.Body, cancellationToken), cancellationToken);
        }

        ValueTask OnFindNodeReplyAsync(Socket socket, in KIpEndpoint endpoint, ulong magic, in KFindNodeResponse<TKNodeId> response, CancellationToken cancellationToken)
        {
            logger?.LogDebug("Sending {Operation}:{Magic} reply to {Endpoint}.", "FIND_NODE", magic, endpoint);
            var b = new ArrayBufferWriter<byte>();
            encoder.Encode(this, b, PackageMessage(magic, response));
            return SocketSendToAsync(socket, b, endpoint, cancellationToken);
        }

        /// <summary>
        /// Invoked when a FIND_VALUE request is received.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask OnReceiveFindValueRequestAsync(Socket socket, KIpEndpoint endpoint, KMessage<TKNodeId, KFindValueRequest<TKNodeId>> request, CancellationToken cancellationToken)
        {
            logger?.LogDebug("Received {Operation}:{Magic} from {Sender} at {Endpoint}.", "FIND_VALUE", request.Header.Magic, request.Header.Sender, endpoint);
            await OnFindValueReplyAsync(socket, endpoint, request.Header.Magic, await engine.OnFindValueAsync(request.Header.Sender, CreateEndpoint(endpoint), request.Body, cancellationToken), cancellationToken);
        }

        ValueTask OnFindValueReplyAsync(Socket socket, in KIpEndpoint endpoint, ulong magic, in KFindValueResponse<TKNodeId> response, CancellationToken cancellationToken)
        {
            logger?.LogDebug("Sending {Operation}:{Magic} reply to {Endpoint}.", "FIND_VALUE", magic, endpoint);
            var b = new ArrayBufferWriter<byte>();
            encoder.Encode(this, b, PackageMessage(magic, response));
            return SocketSendToAsync(socket, b, endpoint, cancellationToken);
        }

        /// <summary>
        /// Invoked with a PING response is received.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="endpoint"></param>
        /// <param name="response"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask OnReceivePingResponseAsync(Socket socket, in KIpEndpoint endpoint, KMessage<TKNodeId, KPingResponse<TKNodeId>> response, CancellationToken cancellationToken)
        {
            pingQueue.Respond(response.Header.Magic, new KResponse<TKNodeId, KPingResponse<TKNodeId>>(CreateEndpoint(endpoint), response.Header.Sender, KResponseStatus.Success, response.Body));
            return new ValueTask(Task.CompletedTask);
        }

        /// <summary>
        /// Invoked with a STORE response is received.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="endpoint"></param>
        /// <param name="response"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask OnReceiveStoreResponseAsync(Socket socket, in KIpEndpoint endpoint, KMessage<TKNodeId, KStoreResponse<TKNodeId>> response, CancellationToken cancellationToken)
        {
            storeQueue.Respond(response.Header.Magic, new KResponse<TKNodeId, KStoreResponse<TKNodeId>>(CreateEndpoint(endpoint), response.Header.Sender, KResponseStatus.Success, response.Body));
            return new ValueTask(Task.CompletedTask);
        }

        /// <summary>
        /// Invoked with a FIND_NODE response is received.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="endpoint"></param>
        /// <param name="response"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask OnReceiveFindNodeResponseAsync(Socket socket, in KIpEndpoint endpoint, KMessage<TKNodeId, KFindNodeResponse<TKNodeId>> response, CancellationToken cancellationToken)
        {
            findNodeQueue.Respond(response.Header.Magic, new KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>(CreateEndpoint(endpoint), response.Header.Sender, KResponseStatus.Success, response.Body));
            return new ValueTask(Task.CompletedTask);
        }

        /// <summary>
        /// Invoked with a FIND_VALUE response is received.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="endpoint"></param>
        /// <param name="response"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask OnReceiveFindValueResponseAsync(Socket socket, in KIpEndpoint endpoint, KMessage<TKNodeId, KFindValueResponse<TKNodeId>> response, CancellationToken cancellationToken)
        {
            findValueQueue.Respond(response.Header.Magic, new KResponse<TKNodeId, KFindValueResponse<TKNodeId>>(CreateEndpoint(endpoint), response.Header.Sender, KResponseStatus.Success, response.Body));
            return new ValueTask(Task.CompletedTask);
        }

        /// <summary>
        /// Stops the network.
        /// </summary>
        /// <returns></returns>
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            using (await sync.LockAsync(cancellationToken))
            {
                // stop listening for address changes
                NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;

                // remove any endpoints registered by ourselves
                foreach (var i in engine.SelfData.Endpoints.Where(i => i.Protocol == this).ToList())
                    engine.SelfData.Endpoints.Remove(i);

                // zero out our endpoint list
                endpoints.Clear();

                // dispose of each socket
                foreach (var socket in recvSockets.Values)
                {
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
                    }
                }

                // zero out sockets
                recvSockets.Clear();
            }
        }

        /// <summary>
        /// Initiates a send of the buffered data to the endpoint.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="target"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask SocketSendToAsync(Socket socket, ArrayBufferWriter<byte> buffer, KIpEndpoint target, CancellationToken cancellationToken)
        {
            var z = new byte[buffer.WrittenCount];
            buffer.WrittenSpan.CopyTo(z);
            return new ValueTask(socket.SendToAsync(new ArraySegment<byte>(z), SocketFlags.None, target.ToIPEndPoint()));
        }

        /// <summary>
        /// Sends the given buffer to an endpoint and begins a wait on the specified reply queue.
        /// </summary>
        /// <typeparam name="TResponseData"></typeparam>
        /// <param name="socket"></param>
        /// <param name="endpoint"></param>
        /// <param name="magic"></param>
        /// <param name="queue"></param>
        /// <param name="buffer"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KResponse<TKNodeId, TResponseData>> SendAndWaitAsync<TResponseData>(Socket socket, KIpEndpoint endpoint, ulong magic, KResponseQueue<TKNodeId, TResponseData, ulong> queue, ArrayBufferWriter<byte> buffer, CancellationToken cancellationToken)
            where TResponseData : struct, IKResponseData<TKNodeId>
        {
            logger?.LogDebug("Queuing response wait for {Magic} to {Endpoint}.", magic, endpoint);

            var c = new CancellationTokenSource();
            var t = queue.WaitAsync(magic, CancellationTokenSource.CreateLinkedTokenSource(c.Token, cancellationToken).Token);

            try
            {
                logger?.LogDebug("Sending packet to {Endpoint} with {Magic}.", endpoint, magic);
                await SocketSendToAsync(socket, buffer, endpoint, cancellationToken);
            }
            catch (Exception)
            {
                // cancel item in response queue
                c.Cancel();
            }

            // wait on response
            var r = await t;
            logger?.LogDebug("Exited wait for {Magic} to {Endpoint}.", magic, endpoint);
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
            var m = NewMagic();
            var b = new ArrayBufferWriter<byte>();
            encoder.Encode(this, b, PackageMessage(m, request));
            return await SendAndWaitAsync(sendSocket, ((KIpProtocolEndpoint<TKNodeId>)endpoint).Endpoint, m, pingQueue, b, cancellationToken);
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
            var m = NewMagic();
            var b = new ArrayBufferWriter<byte>();
            encoder.Encode(this, b, PackageMessage(m, request));
            return await SendAndWaitAsync(sendSocket, ((KIpProtocolEndpoint<TKNodeId>)endpoint).Endpoint, m, storeQueue, b, cancellationToken);
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
            var m = NewMagic();
            var b = new ArrayBufferWriter<byte>();
            encoder.Encode(this, b, PackageMessage(m, request));
            return await SendAndWaitAsync(sendSocket, ((KIpProtocolEndpoint<TKNodeId>)endpoint).Endpoint, m, findNodeQueue, b, cancellationToken);
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
            var m = NewMagic();
            var b = new ArrayBufferWriter<byte>();
            encoder.Encode(this, b, PackageMessage(m, request));
            return await SendAndWaitAsync(sendSocket, ((KIpProtocolEndpoint<TKNodeId>)endpoint).Endpoint, m, findValueQueue, b, cancellationToken);
        }

    }

}
