using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Cogito.Kademlia.Network;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cogito.Kademlia.Stun
{

    /// <summary>
    /// Provides additional endpoints for STUN-derived addresses.
    /// </summary>
    public class KIpStunProvider<TNodeId> : IHostedService
        where TNodeId : unmanaged
    {

        readonly IOptions<KIpStunOptions> options;
        readonly IKHost<TNodeId> host;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="host"></param>
        public KIpStunProvider(IOptions<KIpStunOptions> options, IKHost<TNodeId> host)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        void OnEndpointsCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {

        }

#if NETSTANDARD2_1

        public IAsyncEnumerable<KIpEndpoint> GetEndpointsAsync(Socket socket)
        {
            foreach (var server in options.Value.Servers)
            {
                if (STUN.STUNClient.TryParseHostAndPort(server, out var ep))
                {
                    var a = await STUN.STUNClient.QueryAsync(socket, ep, STUN.STUNQueryType.PublicIP);
                    if (a.QueryError == STUN.STUNQueryError.Success)
                        yield return a.PublicEndPoint;
                }
            }
        }

#endif

        public async Task<IEnumerable<KIpEndpoint>> GetEndpointsEnumerableAsync(Socket socket)
        {
            var l = new List<KIpEndpoint>();

            foreach (var server in options.Value.Servers)
            {
                if (STUN.STUNClient.TryParseHostAndPort(server, out var ep))
                {
                    var a = await STUN.STUNClient.QueryAsync(socket, ep, STUN.STUNQueryType.PublicIP);
                    if (a.QueryError == STUN.STUNQueryError.Success)
                        l.Add(a.PublicEndPoint);
                }
            }

            return l;
        }

    }

}
