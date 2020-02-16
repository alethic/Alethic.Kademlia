namespace Cogito.Kademlia.Network
{

    /// <summary>
    /// Describes the data to send for a ping request.
    /// </summary>
    public struct KFindNodeRequestDatagram<TKNodeId> : IKRequestDatagram
        where TKNodeId : struct, IKNodeId<TKNodeId>
    {

        readonly uint magic;
        readonly TKNodeId nodeId;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="magic"></param>
        /// <param name="nodeId"></param>
        public KFindNodeRequestDatagram(uint magic, TKNodeId nodeId)
        {
            this.magic = magic;
            this.nodeId = nodeId;
        }

        public uint Magic => magic;

        public TKNodeId NodeId => nodeId;

    }

}
