namespace Cogito.Kademlia
{

    /// <summary>
    /// Options for the <see cref="KFixedTableRouter{TNodeId}"/> class.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public class KFixedTableRouterOptions<TNodeId>
        where TNodeId : unmanaged
    {

        public int K { get; set; } = 20;

    }

}
