using System;
using System.Buffers;

namespace Cogito.Kademlia.Network.Udp
{

    /// <summary>
    /// Provides methods to read and write UDP datagrams.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public interface IKUdpSerializer<TNodeId>
        where TNodeId : unmanaged
    {

        /// <summary>
        /// Attempts to read a message sequence from the input data.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        KUdpPacket<TNodeId> Read(ReadOnlyMemory<byte> buffer, IKMessageContext<TNodeId> context);

        /// <summary>
        /// Writes a message sequence to the writer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="context"></param>
        /// <param name="format"></param>
        /// <param name="messages"></param>
        void Write(IBufferWriter<byte> writer, IKMessageContext<TNodeId> context, in KMessageSequence<TNodeId> messages);

    }

}