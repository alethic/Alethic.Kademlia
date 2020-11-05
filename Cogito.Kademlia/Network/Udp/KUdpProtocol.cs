using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using Cogito.Kademlia.Core;
using Cogito.Linq;
using Cogito.Threading;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cogito.Kademlia.Network.Udp
{

    /// <summary>
    /// Implements a simple UDP network layer.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public class KUdpProtocol<TNodeId> : IKProtocol<TNodeId>, IHostedService
        where TNodeId : unmanaged
    {

        const uint magic = 0x8954de4d;

        static readonly Random random = new Random();

        readonly IOptions<KUdpOptions<TNodeId>> options;
        readonly IKEngine<TNodeId> engine;
        readonly IKMessageFormat<TNodeId> format;
        readonly IKRequestHandler<TNodeId> handler;
        readonly ILogger logger;

        readonly AsyncLock sync = new AsyncLock();
        readonly Dictionary<KIpEndpoint, KIpProtocolEndpoint<TNodeId>> endpoints = new Dictionary<KIpEndpoint, KIpProtocolEndpoint<TNodeId>>();
        readonly KRequestResponseQueue<TNodeId, ulong> queue;

        Socket sendSocket;
        Dictionary<IPAddress, Socket> recvSockets;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="engine"></param>
        /// <param name="format"></param>
        /// <param name="handler"></param>
        /// <param name="logger"></param>
        public KUdpProtocol(IOptions<KUdpOptions<TNodeId>> options, IKEngine<TNodeId> engine, IKMessageFormat<TNodeId> format, IKRequestHandler<TNodeId> handler, ILogger logger)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
            this.format = format ?? throw new ArgumentNullException(nameof(format));
            this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            queue = new KRequestResponseQueue<TNodeId, ulong>(options.Value.Timeout);
        }

        /// <summary>
        /// Gets the set of endpoints through which this protocol is reachable.
        /// </summary>
        public IEnumerable<IKProtocolEndpoint<TNodeId>> Endpoints => endpoints.Values.Cast<IKProtocolEndpoint<TNodeId>>();

        /// <summary>
        /// Creates a new <see cref="KIpProtocolEndpoint{TNodeId}"/> for the given <see cref="KIpEndpoint"/>.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        KIpProtocolEndpoint<TNodeId> CreateEndpoint(in KIpEndpoint endpoint, IEnumerable<string> accepts)
        {
            return new KIpProtocolEndpoint<TNodeId>(this, endpoint, KIpProtocolType.Udp, accepts);
        }

        /// <summary>
        /// Creates a new <see cref="KIpProtocolEndpoint{TNodeId}"/> for the given <see cref="KIpEndpoint"/>.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public IKProtocolEndpoint<TNodeId> ResolveEndpoint(Uri uri)
        {
            var n = HttpUtility.ParseQueryString(uri.Query);
            var a = n.Get("format").Split(',');
            return uri.Scheme == "udp" ? CreateEndpoint(KIpEndpoint.Parse(uri.ToString()), a) : null;
        }

        /// <summary>
        /// Gets the next magic value.
        /// </summary>
        /// <returns></returns>
        uint NewReplyId()
        {
            return (uint)random.Next(int.MinValue, int.MaxValue);
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
                            if (j.Address.IsIPv4MappedToIPv6 == false && j.Address.IsIPv6Multicast == false && j.Address.IsIPv6SiteLocal == false && j.Address.IsIPv6LinkLocal == false)
                                if (h.Add(j.Address))
                                    yield return j.Address;
        }

        /// <summary>
        /// Scans for new IP addresses and creates receive sockets.
        /// </summary>
        void RefreshReceiveSockets()
        {
            // determine port, either we already have one allocated previously, or we need to generate a new one
            var recvPort = (ushort?)recvSockets.Select(i => i.Value.LocalEndPoint).Cast<IPEndPoint>().FirstOrDefault()?.Port ?? options.Value.Listen?.Port ?? 0;
            var ipListen = GetLocalIpAddresses();

            // the IPs we listen to are governed by the 'listen' endpoint
            switch (options.Value.Listen?.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    {
                        // listen only on V4
                        ipListen = ipListen.Where(i => i.AddressFamily == AddressFamily.InterNetwork);

                        // listen only on specific address
                        if (options.Value.Listen?.Address != IPAddress.Any)
                            ipListen = ipListen.Where(i => i == options.Value.Listen.Address);
                        break;
                    }

                case AddressFamily.InterNetworkV6:
                    {
                        // listen only on V6
                        ipListen = ipListen.Where(i => i.AddressFamily == AddressFamily.InterNetworkV6);

                        // listen only on specific address
                        if (options.Value.Listen?.Address != IPAddress.IPv6Any)
                            ipListen = ipListen.Where(i => i == options.Value.Listen.Address);
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
                engine.Endpoints.Demote(endpoints[ep] = CreateEndpoint(ep, format.ContentType.Yield()));
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
                    engine.Endpoints.Remove(l.Value);
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
                foreach (var i in endpoints.Values)
                    engine.Endpoints.Remove(i);

                // listen protocol determines send socket binding
                switch (options.Value.Listen?.AddressFamily ?? AddressFamily.Unspecified)
                {
                    case AddressFamily.Unspecified:
                        // establish UDP socket for both IPv4 and IPv6 (dual mode)
                        sendSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                        sendSocket.DualMode = true;
                        sendSocket.Bind(new IPEndPoint(IPAddress.IPv6Any, 0));
                        logger?.LogInformation("Initialized sending UDP socket on {Endpoint}.", sendSocket.LocalEndPoint);
                        break;
                    case AddressFamily.InterNetwork:
                        // establish UDP socket for IPv4
                        sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                        sendSocket.Bind(new IPEndPoint(IPAddress.Any, 0));
                        logger?.LogInformation("Initialized sending UDP socket on {Endpoint}.", sendSocket.LocalEndPoint);
                        break;
                    case AddressFamily.InterNetworkV6:
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
            SocketAsyncEventArgs_Completed((Socket)sender, args);
        }

        /// <summary>
        /// Invoked when a receive operation completes.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="args"></param>
        void SocketAsyncEventArgs_Completed(Socket socket, SocketAsyncEventArgs args)
        {
            logger?.LogTrace("Received incoming packet of {Size} from {Endpoint}.", args.BytesTransferred, (IPEndPoint)args.RemoteEndPoint);

            try
            {
                // some error occurred?
                if (args.SocketError != SocketError.Success)
                    return;

                // we only care about receive from events
                if (args.LastOperation != SocketAsyncOperation.ReceiveFrom)
                    return;

                // no data found
                if (args.BytesTransferred == 0)
                    return;

                // socket is unbound, ignore
                if (socket.IsBound == false)
                    return;

                // extract source endpoint
                var source = new KIpEndpoint((IPEndPoint)args.RemoteEndPoint);

                // check that buffer is a valid packet
                var input = new ReadOnlySpan<byte>(args.Buffer, args.Offset, args.BytesTransferred);
                if (BinaryPrimitives.ReadUInt32LittleEndian(input) != magic)
                    return;

                // advance past magic number
                input = input.Slice(sizeof(uint));

                // format ends at first NUL
                var formatEnd = input.IndexOf((byte)0x00);
                if (formatEnd < 0)
                    return;

                // extract encoded format type
#if NET47 || NETSTANDARD2_0
                var format = Encoding.UTF8.GetString(input.Slice(0, formatEnd).ToArray());
#else
                var format = Encoding.UTF8.GetString(input.Slice(0, formatEnd));
#endif
                if (format == null)
                    return;

                // advance past format
                input = input.Slice(formatEnd + 1);

                // lease temporary memory and copy incoming buffer
                var memown = MemoryPool<byte>.Shared.Rent(input.Length);
                var buffer = memown.Memory.Slice(0, input.Length);
                input.CopyTo(buffer.Span);

                // schedule receive on task pool
                Task.Run(async () =>
                {
                    try
                    {
                        await OnReceiveAsync(socket, source, format, buffer, CancellationToken.None);
                    }
                    catch (Exception e)
                    {
                        logger?.LogError(e, "Unhandled exception dispatching incoming packet.");
                    }
                    finally
                    {
                        // return the buffer to the pool
                        memown.Dispose();
                    }
                });

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
        /// <param name="source"></param>
        /// <param name="formata"></param>
        /// <param name="packet"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask OnReceiveAsync(Socket socket, in KIpEndpoint source, string formata, ReadOnlyMemory<byte> packet, CancellationToken cancellationToken)
        {
            var sequence = format.Decode(new KMessageContext<TNodeId>(engine), new ReadOnlySequence<byte>(packet));
            if (sequence.Network != options.Value.Network)
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
                    KRequest<TNodeId, KPingRequest<TNodeId>> r => OnReceivePingRequestAsync(socket, source, r, cancellationToken),
                    KResponse<TNodeId, KPingResponse<TNodeId>> r => OnReceivePingResponseAsync(socket, source, r, cancellationToken).AsTask(),
                    KRequest<TNodeId, KStoreRequest<TNodeId>> r => OnReceiveStoreRequestAsync(socket, source, r, cancellationToken),
                    KResponse<TNodeId, KStoreResponse<TNodeId>> r => OnReceiveStoreResponseAsync(socket, source, r, cancellationToken).AsTask(),
                    KRequest<TNodeId, KFindNodeRequest<TNodeId>> r => OnReceiveFindNodeRequestAsync(socket, source, r, cancellationToken),
                    KResponse<TNodeId, KFindNodeResponse<TNodeId>> r => OnReceiveFindNodeResponseAsync(socket, source, r, cancellationToken).AsTask(),
                    KRequest<TNodeId, KFindValueRequest<TNodeId>> r => OnReceiveFindValueRequestAsync(socket, source, r, cancellationToken),
                    KResponse<TNodeId, KFindValueResponse<TNodeId>> r => OnReceiveFindValueResponseAsync(socket, source, r, cancellationToken).AsTask(),
                    _ => Task.CompletedTask,
                });
            }

            // return when all complete
            return new ValueTask(Task.WhenAll(todo));
        }

        /// <summary>
        /// Packages new request.
        /// </summary>
        /// <typeparam name="TBody"></typeparam>
        /// <param name="replyId"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        KMessageSequence<TNodeId> PackageRequest<TBody>(uint replyId, TBody body)
            where TBody : struct, IKRequestBody<TNodeId>
        {
            return new KMessageSequence<TNodeId>(options.Value.Network, new[] { (IKRequest<TNodeId>)new KRequest<TNodeId, TBody>(new KMessageHeader<TNodeId>(engine.SelfId, replyId), body) });
        }

        /// <summary>
        /// Packages new reply message.
        /// </summary>
        /// <typeparam name="TBody"></typeparam>
        /// <param name="replyId"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        KMessageSequence<TNodeId> PackageResponse<TBody>(uint replyId, TBody body)
            where TBody : struct, IKResponseBody<TNodeId>
        {
            return new KMessageSequence<TNodeId>(options.Value.Network, new[] { (IKResponse<TNodeId>)new KResponse<TNodeId, TBody>(new KMessageHeader<TNodeId>(engine.SelfId, replyId), KResponseStatus.Failure, body) });
        }

        /// <summary>
        /// Packages new error response.
        /// </summary>
        /// <typeparam name="TBody"></typeparam>
        /// <param name="replyId"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        KMessageSequence<TNodeId> PackageResponse<TBody>(uint replyId, Exception exception)
            where TBody : struct, IKResponseBody<TNodeId>
        {
            return new KMessageSequence<TNodeId>(options.Value.Network, new[] { (IKResponse<TNodeId>)new KResponse<TNodeId, TBody>(new KMessageHeader<TNodeId>(engine.SelfId, replyId), KResponseStatus.Failure, null) });
        }

        /// <summary>
        /// Invoked when a PING request is received.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="source"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task OnReceivePingRequestAsync(Socket socket, in KIpEndpoint source, in KRequest<TNodeId, KPingRequest<TNodeId>> request, CancellationToken cancellationToken)
        {
            logger?.LogDebug("Received {Operation}:{Magic} from {Sender} at {Endpoint}.", "PING", request.Header.ReplyId, request.Header.Sender, source);
            return HandleAsync(socket, source, request, handler.OnPingAsync, cancellationToken);
        }

        /// <summary>
        /// Invoked when a STORE request is received.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task OnReceiveStoreRequestAsync(Socket socket, in KIpEndpoint source, in KRequest<TNodeId, KStoreRequest<TNodeId>> request, CancellationToken cancellationToken)
        {
            logger?.LogDebug("Received {Operation}:{Magic} from {Sender} at {Endpoint}.", "STORE", request.Header.ReplyId, request.Header.Sender, source);
            return HandleAsync(socket, source, request, handler.OnStoreAsync, cancellationToken);
        }

        /// <summary>
        /// Invoked when a FIND_NODE request is received.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="source"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task OnReceiveFindNodeRequestAsync(Socket socket, in KIpEndpoint source, in KRequest<TNodeId, KFindNodeRequest<TNodeId>> request, CancellationToken cancellationToken)
        {
            logger?.LogDebug("Received {Operation}:{Magic} from {Sender} at {Endpoint}.", "FIND_NODE", request.Header.ReplyId, request.Header.Sender, source);
            return HandleAsync(socket, source, request, handler.OnFindNodeAsync, cancellationToken);
        }

        /// <summary>
        /// Invoked when a FIND_VALUE request is received.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="source"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task OnReceiveFindValueRequestAsync(Socket socket, in KIpEndpoint source, in KRequest<TNodeId, KFindValueRequest<TNodeId>> request, CancellationToken cancellationToken)
        {
            logger?.LogDebug("Received {Operation}:{Magic} from {Sender} at {Endpoint}.", "FIND_VALUE", request.Header.ReplyId, request.Header.Sender, source);
            return HandleAsync(socket, source, request, handler.OnFindValueAsync, cancellationToken);
        }

        /// <summary>
        /// Describes a method on the handler.
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="sender"></param>
        /// <param name="source"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        delegate ValueTask<TResponse> HandleAsyncDelegate<TRequest, TResponse>(in TNodeId sender, IKProtocolEndpoint<TNodeId> source, in TRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Handles the specified request.
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="socket"></param>
        /// <param name="source"></param>
        /// <param name="replyId"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        async Task HandleAsync<TRequest, TResponse>(Socket socket, KIpEndpoint source, KRequest<TNodeId, TRequest> request, HandleAsyncDelegate<TRequest, TResponse> handler, CancellationToken cancellationToken)
            where TRequest : struct, IKRequestBody<TNodeId>
            where TResponse : struct, IKResponseBody<TNodeId>
        {
            try
            {
                if (request.Body != null)
                    await ReplyAsync(socket, source, request.Header.ReplyId, await handler(request.Header.Sender, null, request.Body.Value, cancellationToken), cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Unexpected exception handling request.");
                await ReplyAsync<TResponse>(socket, source, request.Header.ReplyId, e, cancellationToken);
            }
        }

        /// <summary>
        /// Sends the specified response.
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="socket"></param>
        /// <param name="source"></param>
        /// <param name="replyId"></param>
        /// <param name="response"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask ReplyAsync<TResponse>(Socket socket, in KIpEndpoint source, uint replyId, in TResponse response, CancellationToken cancellationToken)
            where TResponse : struct, IKResponseBody<TNodeId>
        {
            return SendAsync(socket, source, PackageResponse(replyId, response), cancellationToken);
        }

        /// <summary>
        /// Sends the specified response.
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="socket"></param>
        /// <param name="source"></param>
        /// <param name="replyId"></param>
        /// <param name="response"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask ReplyAsync<TResponse>(Socket socket, in KIpEndpoint source, uint replyId, Exception exception, CancellationToken cancellationToken)
            where TResponse : struct, IKResponseBody<TNodeId>
        {
            return SendAsync(socket, source, PackageResponse<TResponse>(replyId, exception), cancellationToken);
        }

        /// <summary>
        /// Sends the specified response.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="target"></param>
        /// <param name="messages"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask SendAsync(Socket socket, in KIpEndpoint target, KMessageSequence<TNodeId> messages, CancellationToken cancellationToken)
        {
            var b = new ArrayBufferWriter<byte>();
            var m = b.GetSpan(sizeof(uint));
            BinaryPrimitives.WriteUInt32LittleEndian(m, magic);
            b.Advance(sizeof(uint));
            b.Write(Encoding.UTF8.GetBytes(format.ContentType));
            b.Write(new byte[] { 0x00 });
            format.Encode(new KMessageContext<TNodeId>(engine), b, messages);
            return SocketSendToAsync(socket, b, target, cancellationToken);
        }

        /// <summary>
        /// Invoked with a PING response is received.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="source"></param>
        /// <param name="response"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask OnReceivePingResponseAsync(Socket socket, in KIpEndpoint source, in KResponse<TNodeId, KPingResponse<TNodeId>> response, CancellationToken cancellationToken)
        {
            queue.Respond(response.Header.ReplyId, response);
            return new ValueTask(Task.CompletedTask);
        }

        /// <summary>
        /// Invoked with a STORE response is received.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="source"></param>
        /// <param name="response"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask OnReceiveStoreResponseAsync(Socket socket, in KIpEndpoint source, in KResponse<TNodeId, KStoreResponse<TNodeId>> response, CancellationToken cancellationToken)
        {
            queue.Respond(response.Header.ReplyId, response);
            return new ValueTask(Task.CompletedTask);
        }

        /// <summary>
        /// Invoked with a FIND_NODE response is received.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="source"></param>
        /// <param name="response"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask OnReceiveFindNodeResponseAsync(Socket socket, in KIpEndpoint source, in KResponse<TNodeId, KFindNodeResponse<TNodeId>> response, CancellationToken cancellationToken)
        {
            queue.Respond(response.Header.ReplyId, response);
            return new ValueTask(Task.CompletedTask);
        }

        /// <summary>
        /// Invoked with a FIND_VALUE response is received.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="source"></param>
        /// <param name="response"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask OnReceiveFindValueResponseAsync(Socket socket, in KIpEndpoint source, in KResponse<TNodeId, KFindValueResponse<TNodeId>> response, CancellationToken cancellationToken)
        {
            queue.Respond(response.Header.ReplyId, response);
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
                NetworkChange.NetworkAddressChanged -= NetworkChange_NetworkAddressChanged;

                // remove any endpoints registered by ourselves
                foreach (var i in endpoints.Values)
                    engine.Endpoints.Remove(i);

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
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="socket"></param>
        /// <param name="target"></param>
        /// <param name="replyId"></param>
        /// <param name="queue"></param>
        /// <param name="buffer"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KResponse<TNodeId, TResponse>> SendAndWaitAsync<TResponse>(Socket socket, KIpEndpoint target, ulong replyId, KRequestResponseQueue<TNodeId, ulong> queue, ArrayBufferWriter<byte> buffer, CancellationToken cancellationToken)
            where TResponse : struct, IKResponseBody<TNodeId>
        {
            logger?.LogDebug("Queuing response wait for {Magic} to {Endpoint}.", replyId, target);

            var c = new CancellationTokenSource();
            var t = queue.WaitAsync<TResponse>(replyId, CancellationTokenSource.CreateLinkedTokenSource(c.Token, cancellationToken).Token);

            try
            {
                logger?.LogDebug("Sending packet to {Endpoint} with {Magic}.", target, replyId);
                await SocketSendToAsync(socket, buffer, target, cancellationToken);
            }
            catch (Exception)
            {
                // cancel item in response queue
                c.Cancel();
            }

            // wait on response
            var r = await t;
            logger?.LogDebug("Exited wait for {Magic} to {Endpoint}.", replyId, target);
            return r;
        }

        /// <summary>
        /// Invoked to send a PING request.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TNodeId, TResponse>> InvokeAsync<TRequest, TResponse>(IKProtocolEndpoint<TNodeId> target, in TRequest request, CancellationToken cancellationToken)
            where TRequest : struct, IKRequestBody<TNodeId>
            where TResponse : struct, IKResponseBody<TNodeId>
        {
            return target is KIpProtocolEndpoint<TNodeId> t ? InvokeAsync<TRequest, TResponse>(t, request, cancellationToken) : throw new KProtocolException(KProtocolError.Invalid, "Invalid endpoint type for protocol.");
        }

        ValueTask<KResponse<TNodeId, TResponse>> InvokeAsync<TRequest, TResponse>(KIpProtocolEndpoint<TNodeId> target, in TRequest request, CancellationToken cancellationToken)
            where TRequest : struct, IKRequestBody<TNodeId>
            where TResponse : struct, IKResponseBody<TNodeId>
        {
            var b = new ArrayBufferWriter<byte>();

            // write protocol magic
            var m = b.GetSpan(sizeof(uint));
            BinaryPrimitives.WriteUInt32LittleEndian(m, magic);
            b.Advance(sizeof(uint));

            // write format type
            b.Write(Encoding.UTF8.GetBytes(format.ContentType));
            b.Write(new byte[] { 0x00 });

            // write message sequence
            var replyId = NewReplyId();
            format.Encode(new KMessageContext<TNodeId>(engine), b, PackageRequest(replyId, request));

            return SendAndWaitAsync<TResponse>(sendSocket, target.Endpoint, replyId, queue, b, cancellationToken);
        }

    }

}
