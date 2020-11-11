using System;

using MessagePack;

namespace Alethic.Kademlia.MessagePack.Structures
{

    [MessagePackObject]
    public class ValueInfo
    {

        [Key(0)]
        public byte[] Data { get; set; }

        [Key(1)]
        public ulong Version { get; set; }

        [Key(2)]
        public TimeSpan Ttl { get; set; }

    }

}