using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Threading.Tasks;

using Cogito.IO;
using Cogito.Linq;

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;

namespace Cogito.Kademlia.Http.AspNetCore
{

    public class KademliaHttpMiddleware<TNodeId> : IMiddleware
        where TNodeId : unmanaged
    {

        readonly IKHost<TNodeId> host;
        readonly IEnumerable<IKMessageFormat<TNodeId>> formats;
        readonly KHttpProtocol<TNodeId> protocol;
        readonly IServer server;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="formats"></param>
        /// <param name="protocol"></param>
        /// <param name="server"></param>
        public KademliaHttpMiddleware(IKHost<TNodeId> host, IEnumerable<IKMessageFormat<TNodeId>> formats, KHttpProtocol<TNodeId> protocol, IServer server)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
            this.formats = formats ?? throw new ArgumentNullException(nameof(formats));
            this.protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            this.server = server ?? throw new ArgumentNullException(nameof(server));
        }

        /// <summary>
        /// Invoked when a message sequence is submitted against the endpoint.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (server != null && server.Features.Get<IServerAddressesFeature>() is IServerAddressesFeature addresses)
                foreach (var address in addresses.Addresses)
                    if (Uri.TryCreate(address, UriKind.Absolute, out var uri))
                        protocol.RegisterListenUri(uri);

            var messages = await ReadAsync(context);
            if (messages.Network != host.NetworkId)
                throw new KProtocolException(KProtocolError.Invalid, "Invalid message sequence.");

            await ReplyAsync(context, new KMessageSequence<TNodeId>(host.NetworkId, await Task.WhenAll(messages.OfType<IKRequest<TNodeId>>().Select(i => protocol.ReceiveAsync(i, context.RequestAborted)))));
        }

        /// <summary>
        /// Returns the responses.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="responses"></param>
        /// <returns></returns>
        async Task ReplyAsync(HttpContext context, KMessageSequence<TNodeId> responses)
        {
            foreach (var accept in context.Request.Headers.GetCommaSeparatedValues("Accept"))
            {
                if (formats.FirstOrDefault(i => i.ContentType == accept) is IKMessageFormat<TNodeId> format)
                {
                    await WriteAsync(context, format, responses);
                    return;
                }
            }
        }

        /// <summary>
        /// Writes the message sequence to the HTTP response.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="format"></param>
        /// <param name="responses"></param>
        /// <returns></returns>
        async Task WriteAsync(HttpContext context, IKMessageFormat<TNodeId> format, KMessageSequence<TNodeId> responses)
        {
            var w = PipeWriter.Create(context.Response.Body);
            format.Encode(new KMessageContext<TNodeId>(format.ContentType.Yield()), w, responses);
            await w.FlushAsync(context.RequestAborted);
            w.Complete();
        }

        /// <summary>
        /// Attempts to read a message sequence from the input data.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<KMessageSequence<TNodeId>> ReadAsync(HttpContext context)
        {
            var contentType = context.Request.ContentType;
            if (contentType == null)
                throw new KProtocolException(KProtocolError.Invalid, "Request missing content type.");

            var format = formats.FirstOrDefault(i => i.ContentType == contentType);
            if (format == null)
                throw new KProtocolException(KProtocolError.Invalid, $"Unknown format: '{contentType}'.");

            // decode message sequence
            return format.Decode(new KMessageContext<TNodeId>(formats.Select(i => i.ContentType)), new ReadOnlySequence<byte>(await context.Request.Body.ReadAllBytesAsync()));
        }

    }

}