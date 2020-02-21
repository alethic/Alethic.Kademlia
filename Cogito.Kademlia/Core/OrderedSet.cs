using System.Collections;
using System.Collections.Generic;

namespace Cogito.Kademlia.Core
{

    /// <summary>
    /// Provides a set implementation that maintains its insertion order.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class OrderedSet<T> : IOrderedSet<T>
    {

        readonly IEqualityComparer<T> comparer;
        readonly Dictionary<T, LinkedListNode<T>> dict;
        readonly LinkedList<T> list;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public OrderedSet() : this(EqualityComparer<T>.Default)
        {

        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="comparer"></param>
        public OrderedSet(IEqualityComparer<T> comparer)
        {
            this.comparer = comparer;

            dict = new Dictionary<T, LinkedListNode<T>>(comparer);
            list = new LinkedList<T>();
        }

        /// <summary>
        /// Gets whether or not this collection is read only.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Gets the count of items in the set.
        /// </summary>
        public int Count => dict.Count;

        /// <summary>
        /// Gets the first item in the set.
        /// </summary>
        public T First => list.First.Value;

        /// <summary>
        /// Gets the last item in the set.
        /// </summary>
        public T Last => list.Last.Value;

        /// <summary>
        /// Prepends an item to the start of the set if it does not already exist.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool AddFirst(T item)
        {
            // item already exists in set
            if (dict.TryGetValue(item, out var n))
            {
                // item is already first item
                if (comparer.Equals(list.First.Value, item))
                    return false;

                list.Remove(n);
                list.AddFirst(n);
                return true;
            }

            // item is new
            dict.Add(item, list.AddFirst(item));
            return true;
        }

        /// <summary>
        /// Appends an item to the end of the set if it does not already exist.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool AddLast(T item)
        {
            // item already exists in set
            if (dict.TryGetValue(item, out var n))
            {
                // item is already last item
                if (comparer.Equals(list.Last.Value, item))
                    return false;

                list.Remove(n);
                list.AddLast(n);
                return true;
            }

            // item is new
            dict.Add(item, list.AddLast(item));
            return true;
        }

        /// <summary>
        /// Removes all nodes from the set.
        /// </summary>
        public void Clear()
        {
            list.Clear();
            dict.Clear();
        }

        /// <summary>
        /// Removes an item from the set if found.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(T item)
        {
            if (dict.TryGetValue(item, out var node) == false)
                return false;

            dict.Remove(item);
            list.Remove(node);
            return true;
        }

        /// <summary>
        /// Returns <c>true</c> if the item exists.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(T item)
        {
            return dict.ContainsKey(item);
        }

        /// <summary>
        /// Copies the entire <see cref="OrderedSet{T}"/> to a compatible <see cref="T[]"/> starting at the specified index.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }

        void ICollection<T>.Add(T item) => AddLast(item);

        public IEnumerator<T> GetEnumerator() => list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    }

}
