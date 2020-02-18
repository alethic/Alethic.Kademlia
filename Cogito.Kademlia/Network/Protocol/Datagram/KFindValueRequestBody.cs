namespace Cogito.Kademlia.Network.Protocol.Datagram
{

    /// <summary>
    /// Describes the data to send for a FIND_VALUE request.
    /// </summary>
    public readonly ref struct KFindValueRequestBody<TKNodeId>
        where TKNodeId : unmanaged, IKNodeId<TKNodeId>
    {

        readonly TKNodeId key;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key"></param>
        public KFindValueRequestBody(in TKNodeId key)
        {
            this.key = key;
        }

        /// <summary>
        /// Gets the key to find.
        /// </summary>
        public TKNodeId Key => key;

    }

}
