using System;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a node ID in a Kademlia network.
    /// </summary>
    public interface IKNodeId<TKNodeId> : IEquatable<TKNodeId>
        where TKNodeId : IKNodeId<TKNodeId>
    {



    }

}
