using System;

using MessagePack;

namespace Cogito.Kademlia.MessagePack.Structures
{

    [MessagePackObject]
    public class PingResponse : ResponseBody
    {

        [Key(8)]
        public Uri[] Endpoints { get; set; }

    }

}