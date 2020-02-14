namespace Cogito.Kademlia
{

    /// <summary>
    /// Base class of node protocol responses.
    /// </summary>
    public abstract class KNodeResponse
    {

        readonly KNodeResponseStatus status;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="status"></param>
        public KNodeResponse(KNodeResponseStatus status)
        {
            this.status = status;
        }

        /// <summary>
        /// Gets the overall status of the response.
        /// </summary>
        public KNodeResponseStatus Status => status;

    }

}