namespace Cogito.Kademlia
{

    /// <summary>
    /// Represents any request with a body.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    /// <typeparam name="TBody"></typeparam>
    public interface IKRequest<TNodeId, TBody> : IKRequest<TNodeId>, IKMessage<TNodeId, TBody>
        where TNodeId : unmanaged
        where TBody : struct, IKRequestBody<TNodeId>
    {



    }

    /// <summary>
    /// Represents any request.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public interface IKRequest<TNodeId> : IKMessage<TNodeId>
        where TNodeId : unmanaged
    {



    }

}

