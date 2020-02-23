namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes an exception that occurred in a Kademlia protocol implementation.
    /// </summary>
    public class KProtocolException : KException
    {

        readonly KProtocolError error;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="message"></param>
        public KProtocolException(KProtocolError error, string message) :
            base(message)
        {
            this.error = error;
        }

        /// <summary>
        /// Gets the protcol error that occurred.
        /// </summary>
        public KProtocolError Error => error;

    }

}
