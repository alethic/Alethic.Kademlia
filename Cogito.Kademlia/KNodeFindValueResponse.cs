namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a response to a FIND_VALUE request.
    /// </summary>
    public class KNodeFindValueResponse : KNodeResponse
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="status"></param>
        public KNodeFindValueResponse(KNodeResponseStatus status) :
            base(status)
        {

        }

    }

}
