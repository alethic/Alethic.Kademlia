namespace Alethic.Kademlia
{

    /// <summary>
    /// Represents any response with a body.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    /// <typeparam name="TBody"></typeparam>
    public interface IKResponse<TNodeId, TBody> : IKResponse<TNodeId>, IKMessage<TNodeId, TBody>
        where TNodeId : unmanaged
        where TBody : struct, IKResponseBody<TNodeId>
    {



    }

    /// <summary>
    /// Represents any response.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public interface IKResponse<TNodeId> : IKMessage<TNodeId>
        where TNodeId : unmanaged
    {

        KResponseStatus Status { get; }

    }

}