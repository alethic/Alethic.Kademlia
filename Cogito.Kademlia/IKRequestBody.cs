namespace Cogito.Kademlia
{

    /// <summary>
    /// Defines a type that functions as a request body.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public interface IKRequestBody<TNodeId> : IKMessageBody<TNodeId>
        where TNodeId : unmanaged
    {



    }

}
