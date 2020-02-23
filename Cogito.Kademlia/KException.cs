using System;

namespace Cogito.Kademlia
{

    /// <summary>
    /// Describes an exception that occurred in a Kademlia operation.
    /// </summary>
    public class KException : Exception
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="message"></param>
        public KException(string message) :
            base(message)
        {

        }

    }

}
