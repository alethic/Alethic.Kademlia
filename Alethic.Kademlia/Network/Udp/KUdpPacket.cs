using System;

namespace Alethic.Kademlia.Network.Udp
{

    /// <summary>
    /// Represents a deserialized message sequence.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public struct KUdpPacket<TNodeId>
        where TNodeId : unmanaged
    {

        readonly string format;
        readonly KMessageSequence<TNodeId>? messages;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="messages"></param>
        public KUdpPacket(string format, KMessageSequence<TNodeId>? messages)
        {
            this.format = format ?? throw new ArgumentNullException(nameof(format));
            this.messages = messages;
        }

        /// <summary>
        /// Gets the format the message was in.
        /// </summary>
        public string Format => format;

        /// <summary>
        /// Gets the messages.
        /// </summary>
        public KMessageSequence<TNodeId>? Sequence => messages;

    }

}
