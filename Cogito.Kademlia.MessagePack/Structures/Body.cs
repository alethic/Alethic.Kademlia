namespace Cogito.Kademlia.MessagePack.Structures
{

    [global::MessagePack.Union(0, typeof(PingRequest))]
    [global::MessagePack.Union(1, typeof(PingResponse))]
    [global::MessagePack.Union(2, typeof(StoreRequest))]
    [global::MessagePack.Union(3, typeof(StoreResponse))]
    [global::MessagePack.Union(4, typeof(FindNodeRequest))]
    [global::MessagePack.Union(5, typeof(FindNodeResponse))]
    [global::MessagePack.Union(6, typeof(FindValueRequest))]
    [global::MessagePack.Union(7, typeof(FindValueResponse))]
    public abstract class Body
    {



    }

}
