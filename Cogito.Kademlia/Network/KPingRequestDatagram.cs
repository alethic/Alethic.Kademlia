namespace Cogito.Kademlia.Network
{

    /// <summary>
    /// Describes the data to send for a ping request.
    /// </summary>
    public struct KPingRequestDatagram : IKRequestDatagram
    {

        readonly uint magic;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="magic"></param>
        public KPingRequestDatagram(uint magic)
        {
            this.magic = magic;
        }

        public uint Magic => magic;

    }

}
