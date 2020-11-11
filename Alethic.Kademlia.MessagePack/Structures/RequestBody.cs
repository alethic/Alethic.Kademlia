namespace Alethic.Kademlia.MessagePack.Structures
{

    [global::MessagePack.Union(0, typeof(PingRequest))]
    [global::MessagePack.Union(2, typeof(StoreRequest))]
    [global::MessagePack.Union(4, typeof(FindNodeRequest))]
    [global::MessagePack.Union(6, typeof(FindValueRequest))]
    public abstract class RequestBody
    {



    }

}
