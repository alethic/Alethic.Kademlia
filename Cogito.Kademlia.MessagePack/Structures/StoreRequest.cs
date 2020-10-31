using MessagePack;

namespace Cogito.Kademlia.MessagePack.Structures
{

    [MessagePackObject]
    public class StoreRequest : Request
    {

        [Key(0)]
        public byte[] Key { get; set; }

        [Key(1)]
        public StoreRequestMode Mode { get; set; }

        [Key(2)]
        public bool HasValue { get; set; }

        [Key(3)]
        public ValueInfo Value { get; set; }

    }

}