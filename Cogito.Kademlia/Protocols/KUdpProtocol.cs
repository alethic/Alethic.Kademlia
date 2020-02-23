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

namespace Cogito.Kademlia.Protocols
{

    /// <summary>
    /// Implements a simple UDP network layer.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    /// <typeparam name="TKPeerData"></typeparam>
    public class KUdpProtocol<TKNodeId, TKPeerData> : IKIpProtocol<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
        where TKPeerData : IKEndpointProvider<TKNodeId>
    {

        static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);
        static readonly Random rnd = new Random();

        readonly uint networkId;
        readonly IKEngine<TKNodeId, TKPeerData> engine;
        readonly IKMessageDecoder<TKNodeId> decoder;
        readonly IKMessageEncoder<TKNodeId> encoder;
        readonly ushort port;
        readonly ILogger logger;
        readonly AsyncLock sync = new AsyncLock();

        readonly KIpResponseQueue<TKNodeId, KPingResponse<TKNodeId>> pingQueue = new KIpResponseQueue<TKNodeId, KPingResponse<TKNodeId>>(DefaultTimeout);
        readonly KIpResponseQueue<TKNodeId, KStoreResponse<TKNodeId>> storeQueue = new KIpResponseQueue<TKNodeId, KStoreResponse<TKNodeId>>(DefaultTimeout);
        readonly KIpResponseQueue<TKNodeId, KFindNodeResponse<TKNodeId>> findNodeQueue = new KIpResponseQueue<TKNodeId, KFindNodeResponse<TKNodeId>>(DefaultTimeout);
        readonly KIpResponseQueue<TKNodeId, KFindValueResponse<TKNodeId>> findValueQueue = new KIpResponseQueue<TKNodeId, KFindValueResponse<TKNodeId>>(DefaultTimeout);

        readonly Dictionary<KIpEndpoint, KIpProtocolEndpoint<TKNodeId>> endpoints = new Dictionary<KIpEndpoint, KIpProtocolEndpoint<TKNodeId>>();

        Socket socket;
        SocketAsyncEventArgs recvArgs;
        SocketAsyncEventArgs sendArgs;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="networkId"></param>
        /// <param name="engine"></param>
        /// <param name="encoder"></param>
        /// <param name="decoder"></param>
        /// <param name="port"></param>
        /// <param name="logger"></param>
        public KUdpProtocol(uint networkId, IKEngine<TKNodeId, TKPeerData> engine, IKMessageEncoder<TKNodeId> encoder, IKMessageDecoder<TKNodeId> decoder, ushort port = 0, ILogger logger = null)
        {
            this.networkId = networkId;
            this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
            this.encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
            this.decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
            this.port = port;
            this.logger = logger;
        }

        /// <summary>
        /// Uniquely idenfies the traffic for this network from others.
        /// </summary>
        public uint NetworkId => networkId;

        /// <summary>
        /// Gets the engine associated with this protocol.
        /// </summary>
        public IKEngine<TKNodeId> Engine => engine;

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
        uint NewMagic()
        {
            return (uint)rnd.Next(int.MinValue, int.MaxValue);
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
                            if (IPAddress.IsLoopback(j.Address) == false)
                                if (h.Add(j.Address))
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
                if (socket != null)
                    throw new KProtocolException(KProtocolError.Invalid, "Protocol is already started.");

                // establish UDP socket
                logger?.LogInformation("Initializing UDP socket on {Port}.", port);
                socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                socket.DualMode = true;
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, true);
                socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.PacketInformation, true);
                socket.Bind(new IPEndPoint(IPAddress.IPv6Any, port));

                // reset local endpoint intelligence
                endpoints.Clear();
                foreach (var ip in GetLocalIpEndpoints(socket))
                    endpoints[ip] = CreateEndpoint(ip);

                // remove our previous endpoints
                foreach (var i in engine.SelfData.Endpoints.Where(i => i.Protocol == this).ToList())
                    engine.SelfData.Endpoints.Remove(i);

                // add our endpoints
                foreach (var i in endpoints.Values)
                {
                    logger?.LogInformation("Socket listening on {Endpoint}.", i);
                    engine.SelfData.Endpoints.AddLast(i);
                }

                recvArgs = new SocketAsyncEventArgs();
                recvArgs.RemoteEndPoint = new IPEndPoint(IPAddress.IPv6Any, 0);
                recvArgs.SetBuffer(new byte[8192], 0, 8192);
                recvArgs.Completed += recvArgs_Completed;

                sendArgs = new SocketAsyncEventArgs();
                sendArgs.SetBuffer(new byte[8192], 0, 8192);
                sendArgs.Completed += sendArgs_Completed;

                logger?.LogInformation("Waiting for incoming packets.");
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
            // should only be receiving packets from our message loop
            if (args.LastOperation != SocketAsyncOperation.ReceiveMessageFrom)
            {
                logger?.LogTrace("Unexpected packet operation {Operation}.", args.LastOperation);
                return;
            }

            logger?.LogInformation("Received incoming packet of {Size} from {Endpoint}.", args.BytesTransferred, (IPEndPoint)args.RemoteEndPoint);
            var p = new KIpEndpoint((IPEndPoint)args.RemoteEndPoint);
            var b = new ReadOnlySpan<byte>(args.Buffer, args.Offset, args.BytesTransferred);
            var o = MemoryPool<byte>.Shared.Rent(b.Length);
            var m = o.Memory.Slice(0, b.Length);
            b.CopyTo(m.Span);
            Task.Run(async () => { try { await OnReceiveAsync(p, m.Span, CancellationToken.None); } catch { } finally { o.Dispose(); } });

            // continue receiving if socket still available
            // this lock is blocking, but should be okay since this event handler can stall
            using (sync.LockAsync().Result)
            {
                if (socket != null && socket.IsBound)
                {
                    recvArgs.RemoteEndPoint = new IPEndPoint(IPAddress.IPv6Any, 0);
                    socket.ReceiveMessageFromAsync(recvArgs);
                }
            }
        }

        /// <summary>
        /// Invoked when a datagram is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="packet"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask OnReceiveAsync(in KIpEndpoint endpoint, ReadOnlySpan<byte> packet, CancellationToken cancellationToken)
        {
            // check for continued connection
            var s = socket;
            if (s == null || s.IsBound == false)
                return new ValueTask(Task.CompletedTask);

            // decode incoming byte sequence
            var l = decoder.Decode(this, new ReadOnlySequence<byte>(packet.ToArray()));
            if (l.NetworkId != networkId)
            {
                logger?.LogWarning("Received unexpected message sequence for network {NetworkId}.", l.NetworkId);
                return new ValueTask(Task.CompletedTask);
            }

            var t = new List<Task>();

            // dispatch individual messages into infrastructure
            foreach (var m in l)
            {
                t.Add(m switch
                {
                    KMessage<TKNodeId, KPingRequest<TKNodeId>> r => OnReceivePingRequestAsync(endpoint, r, cancellationToken).AsTask(),
                    KMessage<TKNodeId, KPingResponse<TKNodeId>> r => OnReceivePingResponseAsync(endpoint, r, cancellationToken).AsTask(),
                    KMessage<TKNodeId, KStoreRequest<TKNodeId>> r => OnReceiveStoreRequestAsync(endpoint, r, cancellationToken).AsTask(),
                    KMessage<TKNodeId, KStoreResponse<TKNodeId>> r => OnReceiveStoreResponseAsync(endpoint, r, cancellationToken).AsTask(),
                    KMessage<TKNodeId, KFindNodeRequest<TKNodeId>> r => OnReceiveFindNodeRequestAsync(endpoint, r, cancellationToken).AsTask(),
                    KMessage<TKNodeId, KFindNodeResponse<TKNodeId>> r => OnReceiveFindNodeResponseAsync(endpoint, r, cancellationToken).AsTask(),
                    KMessage<TKNodeId, KFindValueRequest<TKNodeId>> r => OnReceiveFindValueRequestAsync(endpoint, r, cancellationToken).AsTask(),
                    KMessage<TKNodeId, KFindValueResponse<TKNodeId>> r => OnReceiveFindValueResponseAsync(endpoint, r, cancellationToken).AsTask(),
                    _ => Task.CompletedTask,
                });
            }

            // return when all complete
            return new ValueTask(Task.WhenAll(t));
        }

        /// <summary>
        /// Packages up a new message originating from this host.
        /// </summary>
        /// <typeparam name="TBody"></typeparam>
        /// <param name="magic"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        KMessageSequence<TKNodeId> PackageMessage<TBody>(uint magic, TBody body)
            where TBody : struct, IKMessageBody<TKNodeId>
        {
            return new KMessageSequence<TKNodeId>(networkId, new[] { (IKMessage<TKNodeId>)new KMessage<TKNodeId, TBody>(new KMessageHeader<TKNodeId>(engine.SelfId, magic), body) });
        }

        /// <summary>
        /// Invoked when a PING request is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask OnReceivePingRequestAsync(KIpEndpoint endpoint, KMessage<TKNodeId, KPingRequest<TKNodeId>> request, CancellationToken cancellationToken)
        {
            logger?.LogDebug("Received PING:{Magic} from {Sender} at {Endpoint}.", request.Header.Magic, request.Header.Sender, endpoint);
            await OnPingReplyAsync(endpoint, request.Header.Magic, await engine.OnPingAsync(request.Header.Sender, CreateEndpoint(endpoint), request.Body, cancellationToken), cancellationToken);
        }

        ValueTask OnPingReplyAsync(in KIpEndpoint endpoint, uint magic, in KPingResponse<TKNodeId> response, CancellationToken cancellationToken)
        {
            logger?.LogDebug("Sending PING:{Magic} reply to {Endpoint}.", magic, endpoint);
            var b = new ArrayBufferWriter<byte>();
            encoder.Encode(this, b, PackageMessage(magic, response));
            return SocketSendToAsync(b, endpoint, cancellationToken);
        }

        /// <summary>
        /// Invoked when a STORE request is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask OnReceiveStoreRequestAsync(KIpEndpoint endpoint, KMessage<TKNodeId, KStoreRequest<TKNodeId>> request, CancellationToken cancellationToken)
        {
            logger?.LogDebug("Received STORE:{Magic} from {Sender} at {Endpoint}.", request.Header.Magic, request.Header.Sender, endpoint);
            await OnStoreReplyAsync(endpoint, request.Header.Magic, await engine.OnStoreAsync(request.Header.Sender, CreateEndpoint(endpoint), request.Body, cancellationToken), cancellationToken);
        }

        ValueTask OnStoreReplyAsync(in KIpEndpoint endpoint, uint magic, in KStoreResponse<TKNodeId> response, CancellationToken cancellationToken)
        {
            logger?.LogDebug("Sending STORE:{Magic} reply to {Endpoint}.", magic, endpoint);
            var b = new ArrayBufferWriter<byte>();
            encoder.Encode(this, b, PackageMessage(magic, response));
            return SocketSendToAsync(b, endpoint, cancellationToken);
        }

        /// <summary>
        /// Invoked when a FIND_NODE request is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask OnReceiveFindNodeRequestAsync(KIpEndpoint endpoint, KMessage<TKNodeId, KFindNodeRequest<TKNodeId>> request, CancellationToken cancellationToken)
        {
            logger?.LogDebug("Received FIND_NODE:{Magic} from {Sender} at {Endpoint}.", request.Header.Magic, request.Header.Sender, endpoint);
            await OnFindNodeReplyAsync(endpoint, request.Header.Magic, await engine.OnFindNodeAsync(request.Header.Sender, CreateEndpoint(endpoint), request.Body, cancellationToken), cancellationToken);
        }

        ValueTask OnFindNodeReplyAsync(in KIpEndpoint endpoint, uint magic, in KFindNodeResponse<TKNodeId> response, CancellationToken cancellationToken)
        {
            logger?.LogDebug("Sending FIND_NODE:{Magic} reply to {Endpoint}.", magic, endpoint);
            var b = new ArrayBufferWriter<byte>();
            encoder.Encode(this, b, PackageMessage(magic, response));
            return SocketSendToAsync(b, endpoint, cancellationToken);
        }

        /// <summary>
        /// Invoked when a FIND_VALUE request is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask OnReceiveFindValueRequestAsync(KIpEndpoint endpoint, KMessage<TKNodeId, KFindValueRequest<TKNodeId>> request, CancellationToken cancellationToken)
        {
            logger?.LogDebug("Received FIND_VALUE:{Magic} from {Sender} at {Endpoint}.", request.Header.Magic, request.Header.Sender, endpoint);
            await OnFindValueReplyAsync(endpoint, request.Header.Magic, await engine.OnFindValueAsync(request.Header.Sender, CreateEndpoint(endpoint), request.Body, cancellationToken), cancellationToken);
        }

        ValueTask OnFindValueReplyAsync(in KIpEndpoint endpoint, uint magic, in KFindValueResponse<TKNodeId> response, CancellationToken cancellationToken)
        {
            logger?.LogDebug("Sending FIND_VALUE:{Magic} reply to {Endpoint}.", magic, endpoint);
            var b = new ArrayBufferWriter<byte>();
            encoder.Encode(this, b, PackageMessage(magic, response));
            return SocketSendToAsync(b, endpoint, cancellationToken);
        }

        /// <summary>
        /// Invoked with a PING response is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="response"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask OnReceivePingResponseAsync(KIpEndpoint endpoint, KMessage<TKNodeId, KPingResponse<TKNodeId>> response, CancellationToken cancellationToken)
        {
            pingQueue.Respond(endpoint, response.Header.Magic, new KResponse<TKNodeId, KPingResponse<TKNodeId>>(CreateEndpoint(endpoint), response.Header.Sender, KResponseStatus.Success, response.Body));
            return new ValueTask(Task.CompletedTask);
        }

        /// <summary>
        /// Invoked with a STORE response is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="response"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask OnReceiveStoreResponseAsync(KIpEndpoint endpoint, KMessage<TKNodeId, KStoreResponse<TKNodeId>> response, CancellationToken cancellationToken)
        {
            storeQueue.Respond(endpoint, response.Header.Magic, new KResponse<TKNodeId, KStoreResponse<TKNodeId>>(CreateEndpoint(endpoint), response.Header.Sender, KResponseStatus.Success, response.Body));
            return new ValueTask(Task.CompletedTask);
        }

        /// <summary>
        /// Invoked with a FIND_NODE response is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="response"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask OnReceiveFindNodeResponseAsync(KIpEndpoint endpoint, KMessage<TKNodeId, KFindNodeResponse<TKNodeId>> response, CancellationToken cancellationToken)
        {
            findNodeQueue.Respond(endpoint, response.Header.Magic, new KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>(CreateEndpoint(endpoint), response.Header.Sender, KResponseStatus.Success, response.Body));
            return new ValueTask(Task.CompletedTask);
        }

        /// <summary>
        /// Invoked with a FIND_VALUE response is received.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="response"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask OnReceiveFindValueResponseAsync(KIpEndpoint endpoint, KMessage<TKNodeId, KFindValueResponse<TKNodeId>> response, CancellationToken cancellationToken)
        {
            findValueQueue.Respond(endpoint, response.Header.Magic, new KResponse<TKNodeId, KFindValueResponse<TKNodeId>>(CreateEndpoint(endpoint), response.Header.Sender, KResponseStatus.Success, response.Body));
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
                // remove any endpoints registered by ourselves
                foreach (var i in engine.SelfData.Endpoints.Where(i => i.Protocol == this).ToList())
                    engine.SelfData.Endpoints.Remove(i);

                // zero out our endpoint list
                endpoints.Clear();

                // shutdown socket
                if (socket != null)
                {
                    // swap for null
                    var s = socket;
                    socket = null;

                    try
                    {
                        s.Close();
                        s.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {

                    }
                }
            }
        }

        /// <summary>
        /// Initiates a send of the buffered data to the endpoint.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="endpoint"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask SocketSendToAsync(ArrayBufferWriter<byte> buffer, KIpEndpoint endpoint, CancellationToken cancellationToken)
        {
            var s = socket;
            if (s == null || s.IsBound == false)
                throw new KProtocolException(KProtocolError.ProtocolNotAvailable, "Cannot send. Socket no longer available.");

            var z = new byte[buffer.WrittenCount];
            buffer.WrittenSpan.CopyTo(z);
            return new ValueTask(s.SendToAsync(new ArraySegment<byte>(z), SocketFlags.None, endpoint.ToIPEndPoint()));
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
            logger?.LogDebug("Queuing response wait for {Magic} to {Endpoint}.", magic, endpoint);

            var c = new CancellationTokenSource();
            var t = queue.WaitAsync(endpoint, magic, CancellationTokenSource.CreateLinkedTokenSource(c.Token, cancellationToken).Token);

            try
            {
                logger?.LogDebug("Sending packet to {Endpoint} with {Magic}.", endpoint, magic);
                await SocketSendToAsync(buffer, endpoint.ToIPEndPoint(), cancellationToken);
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
            var m = NewMagic();
            var b = new ArrayBufferWriter<byte>();
            encoder.Encode(this, b, PackageMessage(m, request));
            return await SendAndWaitAsync(((KIpProtocolEndpoint<TKNodeId>)endpoint).Endpoint, m, storeQueue, b, cancellationToken);
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
            return await SendAndWaitAsync(((KIpProtocolEndpoint<TKNodeId>)endpoint).Endpoint, m, findNodeQueue, b, cancellationToken);
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
            return await SendAndWaitAsync(((KIpProtocolEndpoint<TKNodeId>)endpoint).Endpoint, m, findValueQueue, b, cancellationToken);
        }

    }

}
