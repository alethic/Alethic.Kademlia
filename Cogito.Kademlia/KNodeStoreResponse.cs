namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a response to a STORE request.
    /// </summary>
    public class KNodeStoreResponse : KNodeResponse
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="status"></param>
        public KNodeStoreResponse(KNodeResponseStatus status) :
            base(status)
        {

        }

    }

}
