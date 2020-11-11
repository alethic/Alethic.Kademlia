using MessagePack;

namespace Alethic.Kademlia.MessagePack.Structures
{

    [MessagePackObject]
    public class StoreRequest : RequestBody
    {

        [Key(8)]
        public byte[] Key { get; set; }

        [Key(9)]
        public StoreRequestMode Mode { get; set; }

        [Key(10)]
        public bool HasValue { get; set; }

        [Key(11)]
        public ValueInfo Value { get; set; }

    }

}