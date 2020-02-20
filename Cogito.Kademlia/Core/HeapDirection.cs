namespace Cogito.Kademlia.Core
{

    /// <summary>
    /// Specifies the order in which a Heap will Dequeue items.
    /// </summary>
    public enum HeapDirection
    {

        /// <summary>
        /// Items are Dequeued in Increasing order from least to greatest.
        /// </summary>
        Increasing,

        /// <summary>
        /// Items are Dequeued in Decreasing order, from greatest to least.
        /// </summary>
        Decreasing,

    }

}
