namespace Cogito.Kademlia.Network
{

    /// <summary>
    /// Describes the data to send for a ping request.
    /// </summary>
    public struct KFindValueRequestDatagram<TKNodeId> : IKRequestDatagram
        where TKNodeId : struct, IKNodeId<TKNodeId>
    {

        readonly uint magic;
        readonly TKNodeId key;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="magic"></param>
        /// <param name="key"></param>
        public KFindValueRequestDatagram(uint magic, TKNodeId key)
        {
            this.magic = magic;
            this.key = key;
        }

        public uint Magic => magic;

        public TKNodeId Key => key;

    }

}
