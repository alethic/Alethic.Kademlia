﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Alethic.Kademlia.Core;
using Cogito.Linq;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Alethic.Kademlia.Network.Udp
{

    /// <summary>
    /// Manages the internal operations of UDP connection state.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public class KUdpServer<TNodeId>
        where TNodeId : unmanaged
    {

        static readonly Random random = new Random();

        readonly IKHost<TNodeId> host;
        readonly IEnumerable<IKMessageFormat<TNodeId>> formats;
        readonly IKRequestHandler<TNodeId> handler;
        readonly IKUdpSerializer<TNodeId> serializer;
        readonly ILogger logger;

        readonly KRequestResponseQueue<TNodeId, ulong> queue;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="host"></param>
        /// <param name="formats"></param>
        /// <param name="handler"></param>
        /// <param name="logger"></param>
        public KUdpServer(IOptions<KUdpOptions> options, IKHost<TNodeId> host, IEnumerable<IKMessageFormat<TNodeId>> formats, IKRequestHandler<TNodeId> handler, IKUdpSerializer<TNodeId> serializer, ILogger logger)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
            this.formats = formats ?? throw new ArgumentNullException(nameof(formats));
            this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            queue = new KRequestResponseQueue<TNodeId, ulong>(logger, options.Value.Timeout);
        }

        /// <summary>
        /// Gets the next ReplyId value.
        /// </summary>
        /// <returns></returns>
        uint NewReplyId()
        {
            return (uint)random.Next(int.MinValue, int.MaxValue);
        }

        /// <summary>
        /// Invoke when a datagram is received from a socket.
        /// </summary>
        /// <param name="receive"></param>
        /// <param name="respond"></param>
        /// <param name="args"></param>
        public void OnReceive(Socket receive, Socket respond, SocketAsyncEventArgs args)
        {
            var source = new KIpEndpoint((IPEndPoint)args.RemoteEndPoint);
            logger.LogTrace("Received incoming packet of {Length} from {Endpoint}.", args.BytesTransferred, source);

            try
            {
                // no data found
                if (args.BytesTransferred == 0)
                    return;

                // some error occurred?
                if (args.SocketError != SocketError.Success)
                {
                    logger.LogError("Socket error while receiving UDP data: {SocketError}.", args.SocketError);
                    return;
                }

                // we only care about receive from events
                if (args.LastOperation != SocketAsyncOperation.ReceiveFrom &&
                    args.LastOperation != SocketAsyncOperation.ReceiveMessageFrom)
                {
                    logger.LogError("Unxpected operation while receiving UDP data: {LastOperation}.", args.LastOperation);
                    return;
                }

                // deserialize message sequence
                var packet = serializer.Read(new ReadOnlyMemory<byte>(args.Buffer, args.Offset, args.BytesTransferred), new KMessageContext<TNodeId>(formats.Select(i => i.ContentType)));
                if (packet.Format == null || packet.Sequence == null)
                {
                    logger.LogWarning("Invalid or empty packet.");
                    return;
                }

                Task.Run(async () =>
                {
                    try
                    {
                        logger.LogTrace("Decoded packet as {Format} from {Endpoint}.", packet.Format, source);
                        await OnReceiveAsync(receive, respond, source, packet, CancellationToken.None);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Unhandled exception receiving UDP packet.");
                    }
                });
            }
            catch (Exception e)
            {
                logger.LogError(e, "Exception during UDP receive.");
            }
        }

        /// <summary>
        /// Invoked when a datagram is received.
        /// </summary>
        /// <param name="receive"></param>
        /// <param name="respond"></param>
        /// <param name="source"></param>
        /// <param name="packet"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask OnReceiveAsync(Socket receive, Socket respond, in KIpEndpoint source, in KUdpPacket<TNodeId> packet, CancellationToken cancellationToken)
        {
            if (packet.Sequence.Value.Network != host.NetworkId)
            {
                logger.LogWarning("Received unexpected message sequence for network {NetworkId}.", packet.Sequence.Value.Network);
                return new ValueTask(Task.CompletedTask);
            }

            var todo = new List<Task>(1);

            // dispatch individual messages into infrastructure
            foreach (var message in packet.Sequence.Value)
            {
                todo.Add(message switch
                {
                    KRequest<TNodeId, KPingRequest<TNodeId>> ping => OnReceivePingRequestAsync(receive, respond, source, packet.Format, ping, cancellationToken),
                    KResponse<TNodeId, KPingResponse<TNodeId>> ping => OnReceivePingResponseAsync(receive, respond, source, packet.Format, ping, cancellationToken).AsTask(),
                    KRequest<TNodeId, KStoreRequest<TNodeId>> store => OnReceiveStoreRequestAsync(receive, respond, source, packet.Format, store, cancellationToken),
                    KResponse<TNodeId, KStoreResponse<TNodeId>> store => OnReceiveStoreResponseAsync(receive, respond, source, packet.Format, store, cancellationToken).AsTask(),
                    KRequest<TNodeId, KFindNodeRequest<TNodeId>> findNode => OnReceiveFindNodeRequestAsync(receive, respond, source, packet.Format, findNode, cancellationToken),
                    KResponse<TNodeId, KFindNodeResponse<TNodeId>> findNode => OnReceiveFindNodeResponseAsync(receive, respond, source, packet.Format, findNode, cancellationToken).AsTask(),
                    KRequest<TNodeId, KFindValueRequest<TNodeId>> findValue => OnReceiveFindValueRequestAsync(receive, respond, source, packet.Format, findValue, cancellationToken),
                    KResponse<TNodeId, KFindValueResponse<TNodeId>> findValue => OnReceiveFindValueResponseAsync(receive, respond, source, packet.Format, findValue, cancellationToken).AsTask(),
                    _ => throw new InvalidOperationException(),
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
            return new KMessageSequence<TNodeId>(host.NetworkId, new[] { (IKRequest<TNodeId>)new KRequest<TNodeId, TBody>(new KMessageHeader<TNodeId>(host.SelfId, replyId), body) });
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
            return new KMessageSequence<TNodeId>(host.NetworkId, new[] { (IKResponse<TNodeId>)new KResponse<TNodeId, TBody>(new KMessageHeader<TNodeId>(host.SelfId, replyId), KResponseStatus.Success, body) });
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
            return new KMessageSequence<TNodeId>(host.NetworkId, new[] { (IKResponse<TNodeId>)new KResponse<TNodeId, TBody>(new KMessageHeader<TNodeId>(host.SelfId, replyId), KResponseStatus.Failure, null) });
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
            serializer.Write(b, new KMessageContext<TNodeId>(formats), messages);
            return b.WrittenMemory;
        }

        /// <summary>
        /// Sends the specified response.
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

            return SocketSendToAsync(socket, FormatMessages(messages, formats).Span, target, cancellationToken);
        }

        /// <summary>
        /// Initiates a send of the buffered data to the endpoint.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="buffer"></param>
        /// <param name="endpoint"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask SocketSendToAsync(Socket socket, ReadOnlySpan<byte> buffer, in KIpEndpoint endpoint, CancellationToken cancellationToken)
        {
            return new ValueTask(socket.SendToAsync(new ArraySegment<byte>(buffer.ToArray()), SocketFlags.None, endpoint.ToIPEndPoint()));
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
        Task OnReceivePingRequestAsync(Socket receive, Socket respond, in KIpEndpoint source, string format, in KRequest<TNodeId, KPingRequest<TNodeId>> request, CancellationToken cancellationToken)
        {
            logger.LogDebug("Received {Operation}:{ReplyId} from {Sender} at {Endpoint}.", "PING", request.Header.ReplyId, request.Header.Sender, source);
            return HandleAsync(receive, respond, source, format, request, handler.OnPingAsync, cancellationToken);
        }

        /// <summary>
        /// Invoked when a STORE request is received.
        /// </summary>
        /// <param name="receive"></param>
        /// <param name="respond"></param>
        /// <param name="source"></param>
        /// <param name="format"></param>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task OnReceiveStoreRequestAsync(Socket receive, Socket respond, in KIpEndpoint source, string format, in KRequest<TNodeId, KStoreRequest<TNodeId>> request, CancellationToken cancellationToken)
        {
            logger.LogDebug("Received {Operation}:{ReplyId} from {Sender} at {Endpoint}.", "STORE", request.Header.ReplyId, request.Header.Sender, source);
            return HandleAsync(receive, respond, source, format, request, handler.OnStoreAsync, cancellationToken);
        }

        /// <summary>
        /// Invoked when a FIND_NODE request is received.
        /// </summary>
        /// <param name="receive"></param>
        /// <param name="respond"></param>
        /// <param name="source"></param>
        /// <param name="format"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task OnReceiveFindNodeRequestAsync(Socket receive, Socket respond, in KIpEndpoint source, string format, in KRequest<TNodeId, KFindNodeRequest<TNodeId>> request, CancellationToken cancellationToken)
        {
            logger.LogDebug("Received {Operation}:{ReplyId} from {Sender} at {Endpoint}.", "FIND_NODE", request.Header.ReplyId, request.Header.Sender, source);
            return HandleAsync(receive, respond, source, format, request, handler.OnFindNodeAsync, cancellationToken);
        }

        /// <summary>
        /// Invoked when a FIND_VALUE request is received.
        /// </summary>
        /// <param name="receive"></param>
        /// <param name="respond"></param>
        /// <param name="source"></param>
        /// <param name="format"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task OnReceiveFindValueRequestAsync(Socket receive, Socket respond, in KIpEndpoint source, string format, in KRequest<TNodeId, KFindValueRequest<TNodeId>> request, CancellationToken cancellationToken)
        {
            logger.LogDebug("Received {Operation}:{ReplyId} from {Sender} at {Endpoint}.", "FIND_VALUE", request.Header.ReplyId, request.Header.Sender, source);
            return HandleAsync(receive, respond, source, format, request, handler.OnFindValueAsync, cancellationToken);
        }

        /// <summary>
        /// Describes a method on the handler.
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        delegate ValueTask<TResponse> HandleAsyncDelegate<TRequest, TResponse>(in TNodeId sender, in TRequest request, CancellationToken cancellationToken);

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
                await ReplyAsync(respond, source, format, request.Header.ReplyId, await handler(request.Header.Sender, request.Body.Value, cancellationToken), cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Unexpected exception handling request. Replying with failure.");
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
            queue.Respond(response.Header.ReplyId, response);
            return new ValueTask(Task.CompletedTask);
        }

        /// <summary>
        /// Invoked with a STORE response is received.
        /// </summary>
        /// <param name="receive"></param>
        /// <param name="respond"></param>
        /// <param name="source"></param>
        /// <param name="format"></param>
        /// <param name="response"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask OnReceiveStoreResponseAsync(Socket receive, Socket respond, in KIpEndpoint source, string format, in KResponse<TNodeId, KStoreResponse<TNodeId>> response, CancellationToken cancellationToken)
        {
            queue.Respond(response.Header.ReplyId, response);
            return new ValueTask(Task.CompletedTask);
        }

        /// <summary>
        /// Invoked with a FIND_NODE response is received.
        /// </summary>
        /// <param name="receive"></param>
        /// <param name="respond"></param>
        /// <param name="source"></param>
        /// <param name="format"></param>
        /// <param name="response"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask OnReceiveFindNodeResponseAsync(Socket receive, Socket respond, in KIpEndpoint source, string format, in KResponse<TNodeId, KFindNodeResponse<TNodeId>> response, CancellationToken cancellationToken)
        {
            queue.Respond(response.Header.ReplyId, response);
            return new ValueTask(Task.CompletedTask);
        }

        /// <summary>
        /// Invoked with a FIND_VALUE response is received.
        /// </summary>
        /// <param name="receive"></param>
        /// <param name="respond"></param>
        /// <param name="source"></param>
        /// <param name="format"></param>
        /// <param name="response"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask OnReceiveFindValueResponseAsync(Socket receive, Socket respond, in KIpEndpoint source, string format, in KResponse<TNodeId, KFindValueResponse<TNodeId>> response, CancellationToken cancellationToken)
        {
            queue.Respond(response.Header.ReplyId, response);
            return new ValueTask(Task.CompletedTask);
        }

        /// <summary>
        /// Sends the given buffer to an endpoint and begins a wait on the specified reply queue.
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="socket"></param>
        /// <param name="target"></param>
        /// <param name="replyId"></param>
        /// <param name="buffer"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async ValueTask<KResponse<TNodeId, TResponse>> SendAndWaitAsync<TResponse>(Socket socket, KIpEndpoint target, ulong replyId, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
            where TResponse : struct, IKResponseBody<TNodeId>
        {
            logger.LogTrace("Queuing response wait for {ReplyId} to {Endpoint}.", replyId, target);

            var c = new CancellationTokenSource();
            var t = queue.WaitAsync<TResponse>(replyId, CancellationTokenSource.CreateLinkedTokenSource(c.Token, cancellationToken).Token);

            try
            {
                logger.LogTrace("Sending packet to {Endpoint} with {ReplyId}.", target, replyId);
                await SocketSendToAsync(socket, buffer.Span, target, cancellationToken);
            }
            catch (Exception)
            {
                c.Cancel();
            }

            // wait on response
            var r = await t;
            logger.LogTrace("Exited wait for {ReplyId} to {Endpoint}.", replyId, target);
            return r;
        }

        /// <summary>
        /// Invoked to send a request.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask<KResponse<TNodeId, TResponse>> InvokeAsync<TRequest, TResponse>(Socket socket, KIpProtocolEndpoint<TNodeId> target, in TRequest request, CancellationToken cancellationToken)
            where TRequest : struct, IKRequestBody<TNodeId>
            where TResponse : struct, IKResponseBody<TNodeId>
        {
            if (socket is null)
                throw new ArgumentNullException(nameof(socket));
            if (target is null)
                throw new ArgumentNullException(nameof(target));
            if (target.Formats is null)
                throw new ArgumentException($"Endpoint '{target}' has no acceptable formats.");

            var replyId = NewReplyId();
            return SendAndWaitAsync<TResponse>(socket, target.Endpoint, replyId, FormatMessages(PackageRequest(replyId, request), target.Formats), cancellationToken);
        }

    }

}

