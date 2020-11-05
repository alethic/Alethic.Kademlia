using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
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
        readonly IKEngine<TNodeId> engine;
        readonly IKMessageFormat<TNodeId> format;
        readonly IKConnector<TNodeId> connector;
        readonly IKRequestHandler<TNodeId> handler;
        readonly ILogger logger;

        readonly AsyncLock sync = new AsyncLock();
        readonly KRequestResponseQueue<TNodeId, ulong> queue;

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
        /// <param name="engine"></param>
        /// <param name="format"></param>
        /// <param name="connector"></param>
        /// <param name="handler"></param>
        /// <param name="logger"></param>
        public KUdpMulticastDiscovery(IOptions<KUdpOptions<TNodeId>> options, IKEngine<TNodeId> engine, IKMessageFormat<TNodeId> format, IKConnector<TNodeId> connector, IKRequestHandler<TNodeId> handler, ILogger logger)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
            this.format = format ?? throw new ArgumentNullException(nameof(format));
            this.connector = connector ?? throw new ArgumentNullException(nameof(connector));
            this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            queue = new KRequestResponseQueue<TNodeId, ulong>(options.Value.Multicast.Timeout ?? options.Value.Timeout);
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
        /// Gets the wildcard endpoint.
        /// </summary>
        KIpEndpoint IpAny => options.Value.Multicast.Endpoint.Address.AddressFamily switch
        {
            AddressFamily.InterNetwork => KIpEndpoint.AnyV4,
            AddressFamily.InterNetworkV6 => KIpEndpoint.AnyV6,
            _ => throw new InvalidOperationException(),
        };

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
                        logger?.LogInformation("Initializing IPv4 multicast UDP discovery on {Endpoint}.", options.Value.Multicast.Endpoint);
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
                        logger?.LogInformation("Initializing IPv6 multicast UDP discovery on {Endpoint}.", options.Value.Multicast.Endpoint);
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

                logger?.LogInformation("Waiting for incoming multicast announcement packets.");
                mcastSocket.ReceiveMessageFromAsync(mcastRecvArgs);
                localSocket.ReceiveMessageFromAsync(localRecvArgs);

                // begin new run processes
                runCts = new CancellationTokenSource();
                run = Task.WhenAll(Task.Run(() => ConnectRunAsync(runCts.Token)));
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
            // should only be receiving packets from our message loop
            if (args.LastOperation != SocketAsyncOperation.ReceiveMessageFrom)
            {
                logger?.LogTrace("Unexpected packet operation {Operation}.", args.LastOperation);
                return;
            }

            logger?.LogInformation("Received incoming UDP packet of {Size} from {Endpoint}.", args.BytesTransferred, (IPEndPoint)args.RemoteEndPoint);
            var p = new KIpEndpoint((IPEndPoint)args.RemoteEndPoint);

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

            Task.Run(async () =>
            {
                try
                {
                    await OnReceiveAsync(p, new ReadOnlySequence<byte>(buffer), CancellationToken.None);
                }
                catch (Exception e)
                {
                    logger?.LogError(e, "Unhandled exception dispatching incoming packet.");
                }
                finally
                {
                    memown.Dispose();
                }
            });

            // continue receiving if socket still available
            // this lock is blocking, but should be okay since this event handler can stall
            using (sync.LockAsync().Result)
            {
                // reset remote endpoint
                args.RemoteEndPoint = p.Protocol switch
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
                    logger?.LogInformation("Initiating periodic multicast bootstrap.");
                    await ConnectAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // ignore
                }
                catch (Exception e)
                {
                    logger?.LogError(e, "Unexpected exception occurred during multicast bootstrapping.");
                }

                await Task.Delay(options.Value.Multicast.DiscoveryFrequency, cancellationToken);
            }
        }

        /// <summary>
        /// Attempts to bootstrap the Kademlia engine from the available multicast group members.
        /// </summary>
        /// <returns></returns>
        async ValueTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            // cancel PING after timeout
            cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(new CancellationTokenSource(options.Value.Multicast.Timeout ?? options.Value.Timeout).Token, cancellationToken).Token;

            try
            {
                // ping the received endpoints
                var r = await PingAsync(new KPingRequest<TNodeId>(engine.Endpoints.ToArray()), cancellationToken);
                if (r.Status == KResponseStatus.Failure)
                {
                    logger?.LogError("Unable to PING multicast address");
                    return;
                }

                // initiate connection to received endpoints
                if (r.Body != null)
                    await connector.ConnectAsync(new KEndpointSet<TNodeId>(r.Body.Value.Endpoints), cancellationToken);
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
        /// <param name="buffer"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask OnReceiveAsync(in KIpEndpoint source, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            try
            {
                // check for continued connection
                var s = mcastSocket;
                if (s == null || s.IsBound == false)
                    return new ValueTask(Task.CompletedTask);

                // decode incoming byte sequence
                var l = format.Decode(new KMessageContext<TNodeId>(engine, format.ContentType.Yield()), buffer);
                if (l.Network != options.Value.Network)
                {
                    logger?.LogWarning("Received unexpected message sequence for network {NetworkId}.", l.Network);
                    return new ValueTask(Task.CompletedTask);
                }

                var t = new List<Task>();

                // dispatch individual messages into infrastructure
                foreach (var m in l)
                {
                    // skip messages sent from ourselves
                    if (m.Header.Sender.Equals(engine.SelfId))
                        continue;

                    t.Add(m switch
                    {
                        KRequest<TNodeId, KPingRequest<TNodeId>> r => OnReceivePingRequestAsync(source, r, cancellationToken).AsTask(),
                        KResponse<TNodeId, KPingResponse<TNodeId>> r => OnReceivePingResponseAsync(source, r, cancellationToken).AsTask(),
                        _ => Task.CompletedTask,
                    });
                }

                // return when all complete
                return new ValueTask(Task.WhenAll(t));
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Unexpected exception receiving multicast packet.");
                return new ValueTask(Task.CompletedTask);
            }
        }

        /// <summary>
        /// Initiates a send of the buffered data to the endpoint.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="endpoint"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask SocketSendToAsync(ArrayBufferWriter<byte> buffer, in KIpEndpoint endpoint, CancellationToken cancellationToken)
        {
            var s = localSocket;
            if (s == null)
                throw new KProtocolException(KProtocolError.ProtocolNotAvailable, "Cannot send. Socket no longer available.");

            var z = new byte[buffer.WrittenCount];
            buffer.WrittenSpan.CopyTo(z);
            return new ValueTask(s.SendToAsync(new ArraySegment<byte>(z), SocketFlags.None, endpoint.ToIPEndPoint()));
        }

        /// <summary>
        /// Sends the given buffer to an endpoint and begins a wait on the specified reply queue.
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="replyId"></param>
        /// <param name="queue"></param>
        /// <param name="buffer"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KResponse<TNodeId, TResponse>> SendAndWaitAsync<TResponse>(ulong replyId, KRequestResponseQueue<TNodeId, ulong> queue, ArrayBufferWriter<byte> buffer, CancellationToken cancellationToken)
            where TResponse : struct, IKResponseBody<TNodeId>
        {
            logger?.LogDebug("Queuing response wait for {Magic}.", replyId);

            var c = new CancellationTokenSource();
            var t = queue.WaitAsync<TResponse>(replyId, CancellationTokenSource.CreateLinkedTokenSource(c.Token, cancellationToken).Token);

            try
            {
                logger?.LogDebug("Sending packet to {Endpoint} with {Magic}.", options.Value.Multicast.Endpoint, replyId);
                await SocketSendToAsync(buffer, options.Value.Multicast.Endpoint, cancellationToken);
            }
            catch (Exception)
            {
                // cancel item in response queue
                c.Cancel();
                throw;
            }

            // wait on response
            var r = await t;
            logger?.LogDebug("Exited wait for {Magic} to {Endpoint}.", replyId, IpAny);
            return r;
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
            return new KMessageSequence<TNodeId>(options.Value.Network, new IKRequest<TNodeId>[] { new KRequest<TNodeId, TBody>(new KMessageHeader<TNodeId>(engine.SelfId, replyId), body) });
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
            return new KMessageSequence<TNodeId>(options.Value.Network, new IKResponse<TNodeId>[] { new KResponse<TNodeId, TBody>(new KMessageHeader<TNodeId>(engine.SelfId, replyId), KResponseStatus.Success, body) });
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
            return new KMessageSequence<TNodeId>(options.Value.Network, new IKResponse<TNodeId>[] { new KResponse<TNodeId, TBody>(new KMessageHeader<TNodeId>(engine.SelfId, replyId), KResponseStatus.Failure, null) });
        }

        /// <summary>
        /// Initiates a PING to the multicast endpoint.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<KResponse<TNodeId, KPingResponse<TNodeId>>> PingAsync(KPingRequest<TNodeId> request, CancellationToken cancellationToken)
        {
            try
            {
                var buffer = new ArrayBufferWriter<byte>();

                // write protocol magic
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.GetSpan(sizeof(uint)), magic);
                buffer.Advance(sizeof(uint));

                // write format type
                buffer.Write(Encoding.UTF8.GetBytes(format.ContentType));
                buffer.Write(new byte[] { 0x00 });

                // write message sequence
                var replyId = NewReplyId();
                format.Encode(new KMessageContext<TNodeId>(engine, format.ContentType.Yield()), buffer, PackageMessage(replyId, request));

                // send packet and return task that waits for response
                return SendAndWaitAsync<KPingResponse<TNodeId>>(replyId, queue, buffer, cancellationToken);
            }
            catch (KProtocolException e) when (e.Error == KProtocolError.EndpointNotAvailable)
            {
                logger?.LogError("No response received attempting to ping multicast peers.");
                return default;
            }
        }

        /// <summary>
        /// Invoked when a PING request is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask OnReceivePingRequestAsync(KIpEndpoint endpoint, KRequest<TNodeId, KPingRequest<TNodeId>> request, CancellationToken cancellationToken)
        {
            if (request.Body != null)
            {
                logger?.LogDebug("Received multicast PING:{Magic} from {Sender} at {Endpoint}.", request.Header.ReplyId, request.Header.Sender, endpoint);
                await SendPingReplyAsync(endpoint, request.Header.ReplyId, await handler.OnPingAsync(request.Header.Sender, null, request.Body.Value, cancellationToken), cancellationToken);
            }
        }

        ValueTask SendPingReplyAsync(in KIpEndpoint endpoint, uint replyId, in KPingResponse<TNodeId> response, CancellationToken cancellationToken)
        {
            logger?.LogDebug("Sending multicast PING:{Magic} reply to {Endpoint}.", replyId, endpoint);

            var buffer = new ArrayBufferWriter<byte>();

            // write protocol magic
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.GetSpan(sizeof(uint)), magic);
            buffer.Advance(sizeof(uint));

            // write message format
            buffer.Write(Encoding.UTF8.GetBytes(format.ContentType));
            buffer.Write(new byte[] { 0x00 });

            // write message sequence
            format.Encode(new KMessageContext<TNodeId>(engine, format.ContentType.Yield()), buffer, PackageResponse(replyId, response));

            // send reply packet
            return SocketSendToAsync(buffer, endpoint, cancellationToken);
        }

        /// <summary>
        /// Invoked with a PING response is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="response"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask OnReceivePingResponseAsync(in KIpEndpoint endpoint, in KResponse<TNodeId, KPingResponse<TNodeId>> response, CancellationToken cancellationToken)
        {
            queue.Respond(response.Header.ReplyId, response);
            return new ValueTask(Task.CompletedTask);
        }

    }

}
