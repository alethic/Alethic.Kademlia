namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes the data that comes along with a Kademlia response.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public interface IKResponseBody<TNodeId> : IKMessageBody<TNodeId>
        where TNodeId : unmanaged
    {



    }

}