using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

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
        readonly IKHost<TNodeId> engine;
        readonly IEnumerable<IKMessageFormat<TNodeId>> formats;
        readonly ILogger logger;

        readonly AsyncLock sync = new AsyncLock();
        readonly Dictionary<KIpEndpoint, KIpProtocolEndpoint<TNodeId>> endpoints = new Dictionary<KIpEndpoint, KIpProtocolEndpoint<TNodeId>>();
        readonly KUdpServer<TNodeId> server;

        Socket sendSocket;
        Dictionary<IPAddress, Socket> recvSockets;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="engine"></param>
        /// <param name="formats"></param>
        /// <param name="handler"></param>
        /// <param name="logger"></param>
        public KUdpProtocol(IOptions<KUdpOptions<TNodeId>> options, IKHost<TNodeId> engine, IEnumerable<IKMessageFormat<TNodeId>> formats, IKRequestHandler<TNodeId> handler, ILogger logger)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
            this.formats = formats ?? throw new ArgumentNullException(nameof(formats));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            server = new KUdpServer<TNodeId>(options, engine, formats, handler, new KUdpSerializer<TNodeId>(formats, magic), logger);
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
        KIpProtocolEndpoint<TNodeId> CreateEndpoint(in KIpEndpoint endpoint, IEnumerable<string> formats)
        {
            return new KIpProtocolEndpoint<TNodeId>(this, endpoint, KIpProtocolType.Udp, formats);
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
                engine.Endpoints.Insert(endpoints[ep] = CreateEndpoint(ep, formats.Select(i => i.ContentType)));
                logger.LogInformation("Initialized receiving UDP socket on {Endpoint}.", ep);

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
                logger.LogInformation("Disposing UDP socket for {Endpoint}.", i.Key);
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
                        logger.LogInformation("Initialized sending UDP socket on {Endpoint}.", sendSocket.LocalEndPoint);
                        break;
                    case AddressFamily.InterNetwork:
                        // establish UDP socket for IPv4
                        sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                        sendSocket.Bind(new IPEndPoint(IPAddress.Any, 0));
                        logger.LogInformation("Initialized sending UDP socket on {Endpoint}.", sendSocket.LocalEndPoint);
                        break;
                    case AddressFamily.InterNetworkV6:
                        // establish UDP socket for IPv6
                        sendSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                        sendSocket.Bind(new IPEndPoint(IPAddress.IPv6Any, 0));
                        logger.LogInformation("Initialized sending UDP socket on {Endpoint}.", sendSocket.LocalEndPoint);
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
            if (socket.IsBound == false)
                return;

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

            try
            {
                // queue wait for packet
                if (socket.ReceiveFromAsync(args) == false)
                    SocketAsyncEventArgs_Completed(socket, args);
            }
            catch (ObjectDisposedException)
            {
                // ignore, socket closed
            }
        }

        /// <summary>
        /// Invoked when a receive operation completes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void SocketAsyncEventArgs_Completed(object sender, SocketAsyncEventArgs args)
        {
            var socket = (Socket)sender;
            server.OnReceive(socket, (Socket)sender, args);
            BeginReceive(socket, args);
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
            return server.InvokeAsync<TRequest, TResponse>(sendSocket, target, request, cancellationToken);
        }

    }

}
