using System;
using System.Collections.Generic;
using System.Linq;

using Cogito.Collections;

namespace Cogito.Kademlia.Core
{

    public class PriorityQueue<T> : IQueue<T>
    {

        const int DefaultCapacity = 11;
        const int MaxArraySize = int.MaxValue - 8;

        readonly IComparer<T> comparer;
        T[] queue;
        int count;
        int mods;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="comparer"></param>
        public PriorityQueue(int capacity, IComparer<T> comparer)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));

            this.queue = new T[capacity];
            this.comparer = comparer;
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="capacity"></param>
        public PriorityQueue(int capacity) :
            this(capacity, Comparer<T>.Default)
        {

        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public PriorityQueue() :
            this(DefaultCapacity)
        {

        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="source"></param>
        public PriorityQueue(IEnumerable<T> source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            if (source is SortedSet<T> s)
            {
                this.comparer = s.Comparer;
                InitElementsFromEnumerable(s);
            }
            else if (comparer is PriorityQueue<T> q)
            {
                this.comparer = q.comparer;
                InitFromPriorityQueue(q);
            }
            else
            {
                this.comparer = Comparer<T>.Default;
                InitFromEnumerable(source);
            }
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="comparer"></param>
        public PriorityQueue(IEnumerable<T> source, IComparer<T> comparer)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (comparer is null)
                throw new ArgumentNullException(nameof(comparer));

            this.comparer = comparer ?? Comparer<T>.Default;
            InitFromEnumerable(source);
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="source"></param>
        public PriorityQueue(PriorityQueue<T> source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            this.comparer = source.comparer;
            InitFromPriorityQueue(source);
        }

        /// <summary>
        /// Initializes the elements from the given source.
        /// </summary>
        /// <param name="source"></param>
        public PriorityQueue(SortedSet<T> source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            this.comparer = source.Comparer;
            InitElementsFromEnumerable(source);
        }

        /// <summary>
        /// Initializes the elements from the given source.
        /// </summary>
        /// <param name="source"></param>
        void InitFromPriorityQueue(PriorityQueue<T> source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            this.queue = source.ToArray();
            this.count = source.Count;
        }

        /// <summary>
        /// Initializes the elements from the given source.
        /// </summary>
        /// <param name="source"></param>
        void InitElementsFromEnumerable(IEnumerable<T> source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var a = source.ToArray();
            this.queue = a;
            this.count = a.Length;
        }

        /// <summary>
        /// Initializes the queue from the given source.
        /// </summary>
        /// <param name="source"></param>
        void InitFromEnumerable(IEnumerable<T> source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            InitElementsFromEnumerable(source);
            Heapify();
        }

        /// <summary>
        /// Gets the comparer used to order elements within the queue.
        /// </summary>
        public IComparer<T> Comparer => comparer;

        /// <summary>
        /// Gets the number of items currently in the queue.
        /// </summary>
        public int Count => count;

        /// <summary>
        /// Increases the capacity of the array.
        /// </summary>
        /// <param name="minCapacity"></param>
        void Grow(int minCapacity)
        {
            // double size if small; else grow by 50%
            var oldCapacity = queue.Length;
            var newCapacity = oldCapacity + ((oldCapacity < 64) ? (oldCapacity + 2) : (oldCapacity >> 2));

            // check for overflow
            if (newCapacity - MaxArraySize > 0)
                newCapacity = HugeCapacity(minCapacity);

            var q = new T[newCapacity];
            queue.CopyTo(q, newCapacity);
            this.queue = q;
        }

        int HugeCapacity(int minCapacity)
        {
            if (minCapacity < 0)
                throw new OutOfMemoryException();

            return (minCapacity > MaxArraySize) ? int.MaxValue : MaxArraySize;
        }

        /// <summary>
        /// Adds the specified item to the queue.
        /// </summary>
        /// <param name="item"></param>
        public void Enqueue(T item)
        {
            mods++;
            var i = count;
            if (i >= queue.Length)
                Grow(i + 1);
            count = i + 1;
            if (i == 0)
                queue[0] = item;
            else
                SiftUp(i, item);
        }

        /// <summary>
        /// Returns the item from the top of the queue without removing it.
        /// </summary>
        /// <returns></returns>
        public T Peek()
        {
            if (count == 0)
                throw new InvalidOperationException();
            else
                return queue[0];
        }

        /// <summary>
        /// Returns the index of the item within the queue.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        int IndexOf(T item)
        {
            if (item != null)
                for (int i = 0; i < count; i++)
                    if (item.Equals(queue[i]))
                        return i;

            return -1;
        }

        /// <summary>
        /// Removes the specified item from the queue.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(T item)
        {
            var i = IndexOf(item);
            if (i == -1)
                return false;
            else
            {
                RemoveAt(i);
                return true;
            }
        }

        /// <summary>
        /// Removes the item from the queue based on reference equality.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        bool RemoveEq(T o)
        {
            for (int i = 0; i < count; i++)
            {
                if (ReferenceEquals(o, queue[i]))
                {
                    RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns <c>true</c> if this queue contains the specified item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(T item)
        {
            return IndexOf(item) != -1;
        }

        /// <summary>
        /// Clears the queue.
        /// </summary>
        public void Clear()
        {
            mods++;
#if NET47 || NETSTANDARD2_0
            Array.Clear(queue, 0, queue.Length);
#else
            Array.Fill(queue, default);
#endif
            count = 0;
        }

        /// <summary>
        /// Removes and returns the object at the beginning of the queue.
        /// </summary>
        /// <returns></returns>
        public T Dequeue()
        {
            if (count == 0)
                throw new InvalidOperationException();

            var s = --count;
            mods++;
            var result = queue[0];
            var x = queue[s];
            queue[s] = default;
            if (s != 0)
                SiftDown(0, x);

            return result;
        }

        /// <summary>
        /// Removes the item at the specified position.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        T RemoveAt(int i)
        {
            if (i < 0 || i >= count)
                throw new ArgumentOutOfRangeException(nameof(i));

            mods++;
            int s = --count;
            if (s == i) // removed last element
                queue[i] = default;
            else
            {
                var moved = queue[s];
                queue[s] = default;
                SiftDown(i, moved);
                if (ReferenceEquals(queue[i], moved))
                {
                    SiftUp(i, moved);
                    if (!ReferenceEquals(queue[i], moved))
                        return moved;
                }
            }

            return default;
        }

        void SiftUp(int k, T x)
        {
            while (k > 0)
            {
                var parent = (int)((uint)(k - 1) >> 1);
                var e = queue[parent];
                if (comparer.Compare(x, e) >= 0)
                    break;
                queue[k] = e;
                k = parent;
            }

            queue[k] = x;
        }

        void SiftDown(int k, T x)
        {
            var half = (int)((uint)count >> 1);
            while (k < half)
            {
                var child = (k << 1) + 1;
                var c = queue[child];
                var right = child + 1;
                if (right < count &&
                    comparer.Compare(c, queue[right]) > 0)
                    c = queue[child = right];
                if (comparer.Compare(x, c) <= 0)
                    break;
                queue[k] = c;
                k = child;
            }
            queue[k] = x;
        }

        /// <summary>
        /// Rebuilds the heap.
        /// </summary>
        void Heapify()
        {
            for (int i = (int)((uint)count >> 1) - 1; i >= 0; i--)
                SiftDown(i, queue[i]);
        }

        /// <summary>
        /// Returns the items of the queue as an array.
        /// </summary>
        /// <returns></returns>
        public T[] ToArray()
        {
            var a = new T[count];
            queue.CopyTo(a, 0);
            return a;
        }

    }

}
