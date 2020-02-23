using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Cogito.Kademlia.Core
{

    /// <summary>
    /// Provides a set implementation that maintains its insertion order.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public class OrderedDictionary<TKey, TValue> : IOrderedDictionary<TKey, TValue>
    {

        readonly IEqualityComparer<TKey> comparer;
        readonly Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> dict;
        readonly LinkedList<KeyValuePair<TKey, TValue>> list;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public OrderedDictionary() : this(EqualityComparer<TKey>.Default)
        {

        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="comparer"></param>
        public OrderedDictionary(IEqualityComparer<TKey> comparer)
        {
            this.comparer = comparer;

            dict = new Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>(comparer);
            list = new LinkedList<KeyValuePair<TKey, TValue>>();
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
        public KeyValuePair<TKey, TValue> First => list.First.Value;

        /// <summary>
        /// Gets the last item in the set.
        /// </summary>
        public KeyValuePair<TKey, TValue> Last => list.Last.Value;

        /// <summary>
        /// Gets a collection containing the keys in the dictionary.
        /// </summary>
        public ICollection<TKey> Keys => dict.Keys;

        /// <summary>
        /// Gets a collection containing the values in the dictionary.
        /// </summary>
        public ICollection<TValue> Values => dict.Values.Select(i => i.Value.Value).ToArray();

        /// <summary>
        /// Gets or sets the value of a specific key within the dictionary.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TValue this[TKey key]
        {
            get => dict[key].Value.Value;
            set
            {
                if (dict.ContainsKey(key))
                    dict[key].Value = new KeyValuePair<TKey, TValue>(key, value);
                else
                    AddLast(key, value);
            }
        }

        /// <summary>
        /// Prepends an item to the start of the set if it does not already exist.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool AddFirst(TKey key, TValue value)
        {
            // item already exists in set
            if (dict.TryGetValue(key, out var n))
            {
                // item is already first item
                if (comparer.Equals(list.First.Value.Key, key))
                    return false;

                list.Remove(n);
                list.AddFirst(n);
                return true;
            }

            // item is new
            dict.Add(key, list.AddFirst(new KeyValuePair<TKey, TValue>(key, value)));
            return true;
        }

        /// <summary>
        /// Appends an item to the end of the set if it does not already exist.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool AddLast(TKey key, TValue value)
        {
            // item already exists in set
            if (dict.TryGetValue(key, out var n))
            {
                // item is already last item
                if (comparer.Equals(list.Last.Value.Key, key))
                    return false;

                list.Remove(n);
                list.AddLast(n);
                return true;
            }

            // item is new
            dict.Add(key, list.AddLast(new KeyValuePair<TKey, TValue>(key, value)));
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
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Remove(TKey key)
        {
            if (dict.TryGetValue(key, out var node) == false)
                return false;

            dict.Remove(key);
            list.Remove(node);
            return true;
        }

        /// <summary>
        /// Returns <c>true</c> if the item exists.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(TKey key)
        {
            return dict.ContainsKey(key);
        }

        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            AddLast(key, value);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (dict.TryGetValue(key, out var n))
            {
                value = n.Value.Value;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            AddLast(item.Key, item.Value);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return dict.TryGetValue(item.Key, out var n) && Equals(n.Value.Value, item.Value);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            var i = 0;
            foreach (var j in list)
                array[arrayIndex + i++] = new KeyValuePair<TKey, TValue>(j.Key, j.Value);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            return dict.TryGetValue(item.Key, out var n) && Equals(n.Value.Value, item.Value) ? Remove(item.Key) : false;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var i in list)
                yield return new KeyValuePair<TKey, TValue>(i.Key, i.Value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }

}
