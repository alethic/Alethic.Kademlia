using System;
using System.Collections.Generic;

using Cogito.Kademlia.Core;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Defines a standard list of endpoints.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public class KEndpointSet<TKNodeId> : OrderedDictionary<IKEndpoint<TKNodeId>, KEndpointInfo<TKNodeId>>, IKEndpointSet<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public KEndpointSet() :
            base(EqualityComparer<IKEndpoint<TKNodeId>>.Default)
        {

        }

        public KEndpointInfo<TKNodeId> Update(IKEndpoint<TKNodeId> endpoint)
        {
            if (TryGetValue(endpoint, out var info))
                AddFirst(endpoint, info);
            else
                AddFirst(endpoint, info = new KEndpointInfo<TKNodeId>(DateTime.MinValue));

            return info;
        }

        public KEndpointInfo<TKNodeId> Demote(IKEndpoint<TKNodeId> endpoint)
        {
            if (TryGetValue(endpoint, out var info))
                AddLast(endpoint, info);
            else
                AddLast(endpoint, info = new KEndpointInfo<TKNodeId>(DateTime.MinValue));

            return info;
        }

        public KEndpointInfo<TKNodeId> Select(IKEndpoint<TKNodeId> endpoint)
        {
            return TryGetValue(endpoint, out var info) ? info : null;
        }

        KEndpointInfo<TKNodeId> IKEndpointSet<TKNodeId>.Remove(IKEndpoint<TKNodeId> endpoint)
        {
            return TryGetValue(endpoint, out var info) && Remove(endpoint) ? info : null;
        }

        IEnumerator<IKEndpoint<TKNodeId>> IEnumerable<IKEndpoint<TKNodeId>>.GetEnumerator()
        {
            return Keys.GetEnumerator();
        }

    }

}
