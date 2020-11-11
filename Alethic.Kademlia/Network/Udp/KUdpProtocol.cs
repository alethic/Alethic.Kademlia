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

using Cogito.Collections;
using Cogito.Threading;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Alethic.Kademlia.Network.Udp
{

    /// <summary>
    /// Implements a simple UDP network layer.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public class KUdpProtocol<TNodeId> : IKIpProtocol<TNodeId>, IHostedService
        where TNodeId : unmanaged
    {

        const uint magic = 0x8954de4d;

        static readonly Random random = new Random();

        readonly IOptions<KUdpOptions> options;
        readonly IKHost<TNodeId> host;
        readonly IEnumerable<IKMessageFormat<TNodeId>> formats;
        readonly ILogger logger;

        readonly AsyncLock sync = new AsyncLock();
        readonly Dictionary<IPEndPoint, KIpProtocolEndpoint<TNodeId>> endpoints = new Dictionary<IPEndPoint, KIpProtocolEndpoint<TNodeId>>();
        readonly KUdpServer<TNodeId> server;

        Socket socket;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="host"></param>
        /// <param name="formats"></param>
        /// <param name="handler"></param>
        /// <param name="logger"></param>
        public KUdpProtocol(IOptions<KUdpOptions> options, IKHost<TNodeId> host, IEnumerable<IKMessageFormat<TNodeId>> formats, IKRequestHandler<TNodeId> handler, ILogger logger)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.host = host ?? throw new ArgumentNullException(nameof(host));
            this.formats = formats ?? throw new ArgumentNullException(nameof(formats));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            server = new KUdpServer<TNodeId>(options, host, formats, handler, new KUdpSerializer<TNodeId>(formats, magic), logger);
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
        /// Starts the network.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            using (await sync.LockAsync(cancellationToken))
            {
                if (socket != null)
                    throw new KException("UDP protocol is already started.");

                // register ourselves with the host
                host.RegisterProtocol(this);

                // configure receive sockets and update on IP address change
                NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
                await RefreshSocket(cancellationToken);
                await RefreshEndpoints(cancellationToken);
            }
        }

        /// <summary>
        /// Scans for new IP addresses and creates receive sockets.
        /// </summary>
        Task RefreshSocket(CancellationToken cancellationToken)
        {
            if (socket == null)
            {
                var listen = options.Value.Bind ?? new IPEndPoint(IPAddress.IPv6Any, 0);

                // establish UDP socket
                socket = new Socket(listen.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                socket.DualMode = true;
                socket.Bind(listen);

                // begin receiving from socket
                var args = new SocketAsyncEventArgs();
                args.Completed += SocketAsyncEventArgs_Completed;
                BeginReceive(socket, args);

                logger.LogDebug("Initialized UDP socket on {Endpoint}.", socket.LocalEndPoint);
            }

            return Task.CompletedTask;
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
        /// Refreshes the available endpoints.
        /// </summary>
        /// <returns></returns>
        Task RefreshEndpoints(CancellationToken cancellationToken)
        {
            // set of endpoints to keep
            var keepEndpoints = new List<KIpProtocolEndpoint<TNodeId>>();

            // generate endpoint for each local address that is listened to
            foreach (var ip in GetLocalIpAddresses())
            {
                // determine endpoint by matching IP and port
                var ep = ip.AddressFamily switch
                {
                    AddressFamily.InterNetwork => new KIpEndpoint(new KIp4Address(ip), ((IPEndPoint)socket.LocalEndPoint).Port),
                    AddressFamily.InterNetworkV6 => new KIpEndpoint(new KIp6Address(ip), ((IPEndPoint)socket.LocalEndPoint).Port),
                    _ => throw new InvalidOperationException(),
                };

                // find or create new endpoint
                keepEndpoints.Add(endpoints.GetOrDefault(ep) ?? CreateEndpoint(ep, formats.Select(i => i.ContentType)));
            }

            // insert added endpoints
            foreach (var endpoint in keepEndpoints.Except(endpoints.Values))
            {
                logger.LogDebug("Adding UDP endpoint {Endpoint}.", endpoint);
                endpoints[endpoint.Endpoint] = endpoint;
                host.RegisterEndpoint(endpoint.ToUri());
            }

            // remove stale endpoints
            foreach (var endpoint in endpoints.Values.Except(keepEndpoints))
            {
                logger.LogDebug("Remove UDP endpoint {Endpoint}.", endpoint);
                endpoints.Remove(endpoint.Endpoint);
                host.UnregisterEndpoint(endpoint.ToUri());
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Invoked when a local network address changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void NetworkChange_NetworkAddressChanged(object sender, EventArgs args)
        {
            Task.Run(async () =>
            {
                using (sync.LockAsync(CancellationToken.None))
                    await RefreshSocket(CancellationToken.None);
            });
        }

        /// <summary>
        /// Initiates a receive for the specified socket.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="args"></param>
        void BeginReceive(Socket socket, SocketAsyncEventArgs args)
        {
            if (socket == null)
                return;
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
            if (socket != null)
            {
                server.OnReceive(socket, socket, args);
                BeginReceive(socket, args);
            }
        }

        /// <summary>
        /// Stops the network.
        /// </summary>
        /// <returns></returns>
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            using (await sync.LockAsync(cancellationToken))
            {
                if (socket == null)
                    throw new InvalidOperationException("UDP protocol is already stopped.");

                // unregister ourselves with the host
                host.UnregisterProtocol(this);

                // stop listening for address changes
                NetworkChange.NetworkAddressChanged -= NetworkChange_NetworkAddressChanged;

                // remove any endpoints registered by ourselves
                foreach (var endpoint in endpoints.Values)
                    host.UnregisterEndpoint(endpoint.ToUri());

                // zero out our known endpoints
                endpoints.Clear();

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

        /// <summary>
        /// Invoked to send a request.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TNodeId, TResponse>> InvokeAsync<TRequest, TResponse>(KIpProtocolEndpoint<TNodeId> target, in TRequest request, CancellationToken cancellationToken)
            where TRequest : struct, IKRequestBody<TNodeId>
            where TResponse : struct, IKResponseBody<TNodeId>
        {
            return server.InvokeAsync<TRequest, TResponse>(socket, target, request, cancellationToken);
        }

    }

}
