namespace Cogito.Kademlia
{

    /// <summary>
    /// Defines a type that functions as a message body.
    /// </summary>
    /// <typeparam name="TKNodeId"></typeparam>
    public interface IKMessageBody<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {



    }

}
