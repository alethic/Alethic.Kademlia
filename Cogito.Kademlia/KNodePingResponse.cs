namespace Cogito.Kademlia
{

    public class KNodePingResponse : KNodeResponse
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="status"></param>
        public KNodePingResponse(KNodeResponseStatus status) :
            base(status)
        {

        }

    }

}