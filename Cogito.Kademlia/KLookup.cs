using System;
using System.Collections.Generic;
using System.Text;

namespace Cogito.Kademlia
{

    public class KLookup<TKNodeId> : IKLookup<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

    }

}
