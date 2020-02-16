namespace Cogito.Kademlia.Network
{

    public interface IKRequestDatagram
    {

        /// <summary>
        /// Magic value associated with this datagram.
        /// </summary>
        public uint Magic { get; }

    }

}