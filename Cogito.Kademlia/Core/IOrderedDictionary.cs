using System.Collections.Generic;

namespace Cogito.Kademlia.Core
{

    /// <summary>
    /// Describes a set that maintains items in an order.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public interface IOrderedDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {

        /// <summary>
        /// Gets the first item in the set.
        /// </summary>
        KeyValuePair<TKey, TValue> First { get; }

        /// <summary>
        /// Gets the last item in the set.
        /// </summary>
        KeyValuePair<TKey, TValue> Last { get; }

        /// <summary>
        /// Adds an item to the beginning of the set.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool AddFirst(TKey key, TValue value);

        /// <summary>
        /// Adds an item to the end of the set.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool AddLast(TKey key, TValue value);

    }

}
