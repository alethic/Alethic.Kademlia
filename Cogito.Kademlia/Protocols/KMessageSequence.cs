using System.Collections;
using System.Collections.Generic;

namespace Cogito.Kademlia.Protocols
{

    /// <summary>
    /// Describes a sequence of messages that have been decoded.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public readonly struct KMessageSequence<TKNodeId> : IEnumerable<IKMessage<TKNodeId>>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly uint networkId;
        readonly IEnumerable<IKMessage<TKNodeId>> messages;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="networkId"></param>
        /// <param name="messages"></param>
        public KMessageSequence(uint networkId, IEnumerable<IKMessage<TKNodeId>> messages)
        {
            this.networkId = networkId;
            this.messages = messages;
        }

        /// <summary>
        /// Gets the network ID of the message sequence.
        /// </summary>
        public uint NetworkId => networkId;

        /// <summary>
        /// Gets an enumerator of the messages.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<IKMessage<TKNodeId>> GetEnumerator()
        {
            return messages.GetEnumerator();
        }

        /// <summary>
        /// Gets an enumerator of the messages.
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }

}
