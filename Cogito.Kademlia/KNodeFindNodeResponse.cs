namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes a response to a FIND_NODE request.
    /// </summary>
    public class KNodeFindNodeResponse : KNodeResponse
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="status"></param>
        public KNodeFindNodeResponse(KNodeResponseStatus status) :
            base(status)
        {

        }

    }

}
