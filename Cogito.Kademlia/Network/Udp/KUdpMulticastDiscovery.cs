using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Cogito.Kademlia.Core;
using Cogito.Linq;
using Cogito.Threading;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cogito.Kademlia.Network.Udp
{

    /// <summary>
    /// Listens for multicast PING requests on a multicast group and provides Connect operations for joining a UDP Kademlia network.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public class KUdpMulticastDiscovery<TNodeId> : IHostedService
        where TNodeId : unmanaged
    {

        const uint magic = 0x8954de4d;

        static readonly Random random = new Random();

        readonly IOptions<KUdpOptions<TNodeId>> options;
        readonly IKHost<TNodeId> host;
        readonly IEnumerable<IKMessageFormat<TNodeId>> formats;
        readonly IKConnector<TNodeId> connector;
        readonly IKRequestHandler<TNodeId> handler;
        readonly ILogger logger;

        readonly KUdpSerializer<TNodeId> serializer;
        readonly AsyncLock sync = new AsyncLock();

        Socket mcastSocket;
        SocketAsyncEventArgs mcastRecvArgs;

        Socket localSocket;
        SocketAsyncEventArgs localRecvArgs;

        CancellationTokenSource runCts;
        Task run;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="host"></param>
        /// <param name="formats"></param>
        /// <param name="connector"></param>
        /// <param name="handler"></param>
        /// <param name="logger"></param>
        public KUdpMulticastDiscovery(IOptions<KUdpOptions<TNodeId>> options, IKHost<TNodeId> host, IEnumerable<IKMessageFormat<TNodeId>> formats, IKConnector<TNodeId> connector, IKRequestHandler<TNodeId> handler, ILogger logger)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.host = host ?? throw new ArgumentNullException(nameof(host));
            this.formats = formats ?? throw new ArgumentNullException(nameof(formats));
            this.connector = connector ?? throw new ArgumentNullException(nameof(connector));
            this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            serializer = new KUdpSerializer<TNodeId>(formats, magic);
        }

        /// <summary>
        /// Starts listening for announcement packets.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            using (await sync.LockAsync(cancellationToken))
            {
                if (run != null || runCts != null)
                    throw new InvalidOperationException();

                if (mcastSocket != null)
                    throw new KProtocolException(KProtocolError.Invalid, "Discovery is already started.");

                switch (options.Value.Multicast.Endpoint.AddressFamily)
                {
                    case AddressFamily.InterNetwork:
                        logger.LogInformation("Initializing IPv4 multicast UDP discovery on {Endpoint}.", options.Value.Multicast.Endpoint);
                        mcastSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                        mcastSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        mcastSocket.Bind(new IPEndPoint(IPAddress.Any, options.Value.Multicast.Endpoint.Port));
                        mcastSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(options.Value.Multicast.Endpoint.Address, IPAddress.Any));
                        mcastSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 2);
                        mcastRecvArgs = new SocketAsyncEventArgs();
                        mcastRecvArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

                        localSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                        localSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, true);
                        localSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
                        localSocket.Bind(new IPEndPoint(IPAddress.Any, 0));
                        localRecvArgs = new SocketAsyncEventArgs();
                        localRecvArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                        break;
                    case AddressFamily.InterNetworkV6:
                        logger.LogInformation("Initializing IPv6 multicast UDP discovery on {Endpoint}.", options.Value.Multicast.Endpoint);
                        mcastSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                        mcastSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        mcastSocket.Bind(new IPEndPoint(IPAddress.IPv6Any, options.Value.Multicast.Endpoint.Port));
                        mcastSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, new MulticastOption(options.Value.Multicast.Endpoint.Address, IPAddress.IPv6Any));
                        mcastSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastTimeToLive, 2);
                        mcastRecvArgs = new SocketAsyncEventArgs();
                        mcastRecvArgs.RemoteEndPoint = new IPEndPoint(IPAddress.IPv6Any, 0);

                        localSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                        localSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, true);
                        localSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.PacketInformation, true);
                        localSocket.Bind(new IPEndPoint(IPAddress.IPv6Any, 0));
                        localRecvArgs = new SocketAsyncEventArgs();
                        localRecvArgs.RemoteEndPoint = new IPEndPoint(IPAddress.IPv6Any, 0);
                        break;
                    default:
                        throw new InvalidOperationException();
                }

                mcastRecvArgs.SetBuffer(new byte[8192], 0, 8192);
                mcastRecvArgs.Completed += RecvArgs_Completed;
                localRecvArgs.SetBuffer(new byte[8192], 0, 8192);
                localRecvArgs.Completed += RecvArgs_Completed;

                logger.LogInformation("Waiting for incoming multicast announcement packets.");
                mcastSocket.ReceiveMessageFromAsync(mcastRecvArgs);
                localSocket.ReceiveMessageFromAsync(localRecvArgs);

                // begin new run processes
                runCts = new CancellationTokenSource();
                run = Task.WhenAll(Task.Run(() => ConnectRunAsync(runCts.Token)));

                // also connect when endpoints come and go
                host.Endpoints.CollectionChanged += OnEndpointsChanged;
            }
        }

        /// <summary>
        /// Stops the processes of the engine.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            using (await sync.LockAsync(cancellationToken))
            {
                host.Endpoints.CollectionChanged -= OnEndpointsChanged;

                // shutdown socket
                if (mcastSocket != null)
                {
                    // swap for null
                    var s = mcastSocket;
                    mcastSocket = null;

                    try
                    {
                        s.Close();
                        s.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {

                    }
                }

                // shutdown socket
                if (localSocket != null)
                {
                    // swap for null
                    var s = localSocket;
                    localSocket = null;

                    try
                    {
                        s.Close();
                        s.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {

                    }
                }

                if (runCts != null)
                {
                    runCts.Cancel();
                    runCts = null;
                }

                if (run != null)
                {
                    try
                    {
                        await run;
                    }
                    catch (OperationCanceledException)
                    {
                        // ignore
                    }
                }
            }
        }

        /// <summary>
        /// Invoked when the host endpoints change.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void OnEndpointsChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            Task.Run(() => ConnectAsync(CancellationToken.None));
        }

        /// <summary>
        /// Invoked when a receive operation completes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void RecvArgs_Completed(object sender, SocketAsyncEventArgs args)
        {
            RecvArgs_Completed((Socket)sender, args);
        }

        /// <summary>
        /// Invoked when a receive operation completes.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="args"></param>
        void RecvArgs_Completed(Socket socket, SocketAsyncEventArgs args)
        {
            // extract source endpoint
            var source = new KIpEndpoint((IPEndPoint)args.RemoteEndPoint);
            var length = args.BytesTransferred;
            logger.LogTrace("Received incoming UDP packet of {Length} from {Endpoint}.", length, source);

            // should only be receiving packets from our message loop
            if (args.LastOperation != SocketAsyncOperation.ReceiveMessageFrom)
            {
                logger.LogTrace("Unexpected packet operation {Operation}.", args.LastOperation);
                return;
            }
            // some error occurred?
            if (args.SocketError != SocketError.Success)
                return;

            // we only care about receive from events
            if (args.LastOperation != SocketAsyncOperation.ReceiveMessageFrom &&
                args.LastOperation != SocketAsyncOperation.ReceiveFrom)
                return;

            // no data found
            if (args.BytesTransferred == 0)
                return;

            // socket is unbound, ignore
            if (socket.IsBound == false)
                return;

            // deserialize message sequence
            var packet = serializer.Read(new ReadOnlyMemory<byte>(args.Buffer, args.Offset, args.BytesTransferred), new KMessageContext<TNodeId>(host, formats.Select(i => i.ContentType)));
            if (packet.Format == null || packet.Sequence == null)
                return;

            Task.Run(async () =>
            {
                try
                {
                    logger.LogTrace("Decoded packet as {Format} from {Endpoint}.", packet.Format, source);
                    await OnReceiveAsync(socket, source, packet, CancellationToken.None);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Unhandled exception dispatching incoming packet.");
                }
            });

            // continue receiving if socket still available
            // this lock is blocking, but should be okay since this event handler can stall
            using (sync.LockAsync().Result)
            {
                // reset remote endpoint
                args.RemoteEndPoint = source.Protocol switch
                {
                    KIpAddressFamily.IPv4 => new IPEndPoint(IPAddress.Any, 0),
                    KIpAddressFamily.IPv6 => new IPEndPoint(IPAddress.IPv6Any, 0),
                    _ => throw new InvalidOperationException(),
                };

                try
                {
                    socket.ReceiveMessageFromAsync(args);
                }
                catch (ObjectDisposedException)
                {
                    // we must have been terminated, ignore
                }
            }
        }

        /// <summary>
        /// Periodically publishes key/value pairs to the appropriate nodes.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task ConnectRunAsync(CancellationToken cancellationToken)
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                try
                {
                    // no reason to proceed without endpoints
                    if (host.Endpoints.Count == 0)
                        continue;

                    logger.LogInformation("Initiating periodic multicast bootstrap.");
                    await ConnectAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // ignore
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Unexpected exception occurred during multicast bootstrapping.");
                }

                await Task.Delay(options.Value.Multicast.DiscoveryFrequency, cancellationToken);
            }
        }

        /// <summary>
        /// Attempts to bootstrap the Kademlia engine from the available multicast group members.
        /// </summary>
        /// <returns></returns>
        async ValueTask ConnectAsync(CancellationToken cancellationToken)
        {
            try
            {
                await PingAsync(new KPingRequest<TNodeId>(host.Endpoints.ToArray()), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (KProtocolException e) when (e.Error == KProtocolError.EndpointNotAvailable)
            {
                // ignore
            }
        }

        /// <summary>
        /// Invoked when a datagram is received.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="packet"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask OnReceiveAsync(Socket receive, in KIpEndpoint source, in KUdpPacket<TNodeId> packet, CancellationToken cancellationToken)
        {
            if (packet.Sequence.Value.Network != options.Value.Network)
            {
                logger.LogWarning("Received unexpected message sequence for network {NetworkId}.", packet.Sequence.Value.Network);
                return new ValueTask(Task.CompletedTask);
            }

            var todo = new List<Task>();

            // dispatch individual messages into infrastructure
            foreach (var message in packet.Sequence)
            {
                // skip messages sent from ourselves
                if (message.Header.Sender.Equals(host.SelfId))
                    continue;

                todo.Add(message switch
                {
                    KRequest<TNodeId, KPingRequest<TNodeId>> request => OnReceivePingRequestAsync(receive, localSocket, source, packet.Format, request, cancellationToken),
                    KResponse<TNodeId, KPingResponse<TNodeId>> response => OnReceivePingResponseAsync(receive, localSocket, source, packet.Format, response, cancellationToken).AsTask(),
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
        /// <param name="replyId"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        KMessageSequence<TNodeId> PackageMessage<TBody>(uint replyId, TBody body)
            where TBody : struct, IKRequestBody<TNodeId>
        {
            return new KMessageSequence<TNodeId>(options.Value.Network, new IKRequest<TNodeId>[] { new KRequest<TNodeId, TBody>(new KMessageHeader<TNodeId>(host.SelfId, replyId), body) });
        }

        /// <summary>
        /// Packages up a new message originating from this host.
        /// </summary>
        /// <typeparam name="TBody"></typeparam>
        /// <param name="replyId"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        KMessageSequence<TNodeId> PackageResponse<TBody>(uint replyId, TBody body)
            where TBody : struct, IKResponseBody<TNodeId>
        {
            return new KMessageSequence<TNodeId>(options.Value.Network, new IKResponse<TNodeId>[] { new KResponse<TNodeId, TBody>(new KMessageHeader<TNodeId>(host.SelfId, replyId), KResponseStatus.Success, body) });
        }

        /// <summary>
        /// Packages up a new message originating from this host.
        /// </summary>
        /// <typeparam name="TBody"></typeparam>
        /// <param name="replyId"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        KMessageSequence<TNodeId> PackageResponse<TBody>(uint replyId, Exception exception)
            where TBody : struct, IKResponseBody<TNodeId>
        {
            return new KMessageSequence<TNodeId>(options.Value.Network, new IKResponse<TNodeId>[] { new KResponse<TNodeId, TBody>(new KMessageHeader<TNodeId>(host.SelfId, replyId), KResponseStatus.Failure, null) });
        }

        /// <summary>
        /// Serialize the given message sequence into memory.
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="formats"></param>
        /// <returns></returns>
        ReadOnlyMemory<byte> FormatMessages(KMessageSequence<TNodeId> messages, IEnumerable<string> formats)
        {
            if (formats is null)
                throw new ArgumentNullException(nameof(formats));

            var b = new ArrayBufferWriter<byte>();
            serializer.Write(b, new KMessageContext<TNodeId>(host, formats), messages);
            return b.WrittenMemory;
        }

        /// <summary>
        /// Sends the messages.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="target"></param>
        /// <param name="formats"></param>
        /// <param name="messages"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask SendAsync(Socket socket, in KIpEndpoint target, IEnumerable<string> formats, KMessageSequence<TNodeId> messages, CancellationToken cancellationToken)
        {
            if (socket is null)
                throw new ArgumentNullException(nameof(socket));
            if (formats is null)
                throw new ArgumentNullException(nameof(formats));

            return SocketSendToAsync(socket, target, FormatMessages(messages, formats).Span, cancellationToken);
        }

        /// <summary>
        /// Initiates a send of the buffered data to the endpoint.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="target"></param>
        /// <param name="buffer"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask SocketSendToAsync(Socket socket, in KIpEndpoint target, ReadOnlySpan<byte> buffer, CancellationToken cancellationToken)
        {
            return new ValueTask(socket.SendToAsync(new ArraySegment<byte>(buffer.ToArray()), SocketFlags.None, target.ToIPEndPoint()));
        }

        /// <summary>
        /// Initiates a PING to the multicast endpoint.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask PingAsync(in KPingRequest<TNodeId> request, CancellationToken cancellationToken)
        {
            return SendAsync(localSocket, options.Value.Multicast.Endpoint, formats.Select(i => i.ContentType), PackageMessage(0, request), cancellationToken);
        }

        /// <summary>
        /// Invoked when a PING request is received.
        /// </summary>
        /// <param name="receive"></param>
        /// <param name="respond"></param>
        /// <param name="source"></param>
        /// <param name="format"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task OnReceivePingRequestAsync(Socket receive, Socket respond, KIpEndpoint source, string format, KRequest<TNodeId, KPingRequest<TNodeId>> request, CancellationToken cancellationToken)
        {
            logger.LogDebug("Received multicast PING:{Magic} from {Sender} at {Endpoint}.", request.Header.ReplyId, request.Header.Sender, source);
            return HandleAsync(receive, respond, source, format, request, handler.OnPingAsync, cancellationToken);
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
        /// <param name="receive"></param>
        /// <param name="respond"></param>
        /// <param name="source"></param>
        /// <param name="format"></param>
        /// <param name="replyId"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        async Task HandleAsync<TRequest, TResponse>(Socket receive, Socket respond, KIpEndpoint source, string format, KRequest<TNodeId, TRequest> request, HandleAsyncDelegate<TRequest, TResponse> handler, CancellationToken cancellationToken)
            where TRequest : struct, IKRequestBody<TNodeId>
            where TResponse : struct, IKResponseBody<TNodeId>
        {
            if (request.Body == null)
                return;

            try
            {
                await ReplyAsync(respond, source, format, request.Header.ReplyId, await handler(request.Header.Sender, null, request.Body.Value, cancellationToken), cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Unexpected exception handling request.");
                await ReplyAsync<TResponse>(respond, source, format, request.Header.ReplyId, e, cancellationToken);
            }
        }

        /// <summary>
        /// Sends the specified response.
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="socket"></param>
        /// <param name="source"></param>
        /// <param name="format"></param>
        /// <param name="replyId"></param>
        /// <param name="response"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask ReplyAsync<TResponse>(Socket socket, in KIpEndpoint source, string format, uint replyId, in TResponse response, CancellationToken cancellationToken)
            where TResponse : struct, IKResponseBody<TNodeId>
        {
            return SendAsync(socket, source, format.Yield(), PackageResponse(replyId, response), cancellationToken);
        }

        /// <summary>
        /// Sends the specified response.
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="socket"></param>
        /// <param name="source"></param>
        /// <param name="format"></param>
        /// <param name="replyId"></param>
        /// <param name="response"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask ReplyAsync<TResponse>(Socket socket, in KIpEndpoint source, string format, uint replyId, Exception exception, CancellationToken cancellationToken)
            where TResponse : struct, IKResponseBody<TNodeId>
        {
            return SendAsync(socket, source, format.Yield(), PackageResponse<TResponse>(replyId, exception), cancellationToken);
        }

        /// <summary>
        /// Invoked with a PING response is received.
        /// </summary>
        /// <param name="receive"></param>
        /// <param name="respond"></param>
        /// <param name="source"></param>
        /// <param name="format"></param>
        /// <param name="response"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask OnReceivePingResponseAsync(Socket receive, Socket respond, in KIpEndpoint source, string format, in KResponse<TNodeId, KPingResponse<TNodeId>> response, CancellationToken cancellationToken)
        {
            return connector.ConnectAsync(new KEndpointSet<TNodeId>(response.Body.Value.Endpoints), cancellationToken);
        }

    }

}
