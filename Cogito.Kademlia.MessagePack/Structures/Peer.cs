using System;

using MessagePack;

namespace Cogito.Kademlia.MessagePack.Structures
{

    [MessagePackObject]
    public class Peer
    {

        [Key(0)]
        public byte[] Id { get; set; }

        [Key(1)]
        public Uri[] Endpoints { get; set; }

    }

}
