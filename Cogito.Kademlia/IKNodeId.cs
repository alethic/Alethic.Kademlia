using System;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a node ID in a Kademlia network.
    /// </summary>
    public interface IKNodeId<TKNodeId> : IEquatable<TKNodeId>
        where TKNodeId : IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Gets the size of the node ID in bits.
        /// </summary>
        int Size { get; }

        /// <summary>
        /// Writes the value of this node ID to the specified binary output.
        /// </summary>
        /// <param name="other"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        void WriteTo(Span<byte> output);

    }

}
