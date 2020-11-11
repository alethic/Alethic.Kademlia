using System;

using MessagePack;

namespace Alethic.Kademlia.MessagePack.Structures
{

    [MessagePackObject]
    public class PingResponse : ResponseBody
    {

        [Key(8)]
        public Uri[] Endpoints { get; set; }

    }

}