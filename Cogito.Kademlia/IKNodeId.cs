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
        /// Gets the length of the distance value in bits.
        /// </summary>
        int DistanceSize { get; }

        /// <summary>
        /// Calculates the distance between this node ID and the other node ID and outputs it to the specified destination in most signficant byte order.
        /// </summary>
        /// <param name="other"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        void CalculateDistance(TKNodeId other, Span<byte> output);

    }

}
