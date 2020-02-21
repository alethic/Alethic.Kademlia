using System.Collections.Generic;

namespace Cogito.Kademlia.Core
{

    /// <summary>
    /// Describes a set that maintains items in an order.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IOrderedSet<T> : ICollection<T>
    {

        /// <summary>
        /// Gets the first item in the set.
        /// </summary>
        T First { get; }

        /// <summary>
        /// Gets the last item in the set.
        /// </summary>
        T Last { get; }

        /// <summary>
        /// Adds an item to the beginning of the set.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        bool AddFirst(T item);

        /// <summary>
        /// Adds an item to the end of the set.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        bool AddLast(T item);

    }

}
