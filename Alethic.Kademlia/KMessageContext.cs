using System;
using System.Collections.Generic;

namespace Alethic.Kademlia
{

    /// <summary>
    /// Provides information about the message being sent.
    /// </summary>
    /// <typeparam name="TNodeId"></typeparam>
    public class KMessageContext<TNodeId> : IKMessageContext<TNodeId>
        where TNodeId : unmanaged
    {

        readonly IEnumerable<string> formats;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="formats"></param>
        public KMessageContext(IEnumerable<string> formats)
        {
            this.formats = formats ?? throw new ArgumentNullException(nameof(formats));
        }

        /// <summary>
        /// Gets the allowable formats.
        /// </summary>
        public IEnumerable<string> Formats => formats;

    }

}
