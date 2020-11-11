namespace Alethic.Kademlia.MessagePack.Structures
{

    [global::MessagePack.Union(0, typeof(PingResponse))]
    [global::MessagePack.Union(2, typeof(StoreResponse))]
    [global::MessagePack.Union(4, typeof(FindNodeResponse))]
    [global::MessagePack.Union(6, typeof(FindValueResponse))]
    public abstract class ResponseBody
    {



    }

}
