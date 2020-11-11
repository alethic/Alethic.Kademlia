using System;

using MessagePack;

namespace Alethic.Kademlia.MessagePack.Structures
{

    [MessagePackObject]
    public class PingRequest : RequestBody
    {

        [Key(8)]
        public Uri[] Endpoints { get; set; }

    }

}