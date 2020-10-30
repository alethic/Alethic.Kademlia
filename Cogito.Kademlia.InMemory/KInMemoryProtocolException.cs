namespace Cogito.Kademlia.InMemory
{

    public class KInMemoryProtocolException : KProtocolException
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="error"></param>
        /// <param name="message"></param>
        public KInMemoryProtocolException(KProtocolError error, string message) :
            base(error, message)
        {

        }

    }

}