using System;
using System.Collections.Generic;
using System.Diagnostics;

using Cogito.Collections;

namespace Cogito.Kademlia.Core
{

    [DebuggerDisplay("Count = {Count}")]
    public sealed class FibonacciQueue<TVertex, TDistance> : IPriorityQueue<TVertex>
    {

        /// <summary>
        /// Returns the method that implement the access indexer.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public static Func<TKey, TValue> GetIndexer<TKey, TValue>(IDictionary<TKey, TValue> dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            var method = dictionary.GetType().GetProperty("Item").GetGetMethod();
            return (Func<TKey, TValue>)Delegate.CreateDelegate(typeof(Func<TKey, TValue>), dictionary, method, true);
        }

        readonly FibonacciHeap<TDistance, TVertex> heap;
        readonly Dictionary<TVertex, FibonacciHeapCell<TDistance, TVertex>> cells;
        readonly Func<TVertex, TDistance> distances;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="distances"></param>
        public FibonacciQueue(Func<TVertex, TDistance> distances) :
            this(null, distances, Comparer<TDistance>.Default.Compare)
        {
            if (distances == null)
                throw new ArgumentNullException(nameof(distances));
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="valueCount"></param>
        /// <param name="values"></param>
        /// <param name="distances"></param>
        public FibonacciQueue(IEnumerable<TVertex> values, Func<TVertex, TDistance> distances) :
            this(values, distances, Comparer<TDistance>.Default.Compare)
        {
            if (values is null)
                throw new ArgumentNullException(nameof(values));
            if (distances is null)
                throw new ArgumentNullException(nameof(distances));
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="distances"></param>
        /// <param name="comparer"></param>
        public FibonacciQueue(IEnumerable<TVertex> values, Func<TVertex, TDistance> distances, Func<TDistance, TDistance, int> comparer)
        {
            if (distances is null)
                throw new ArgumentNullException(nameof(distances));
            if (comparer is null)
                throw new ArgumentNullException(nameof(comparer));

            this.distances = distances;
            cells = new Dictionary<TVertex, FibonacciHeapCell<TDistance, TVertex>>();
            heap = new FibonacciHeap<TDistance, TVertex>(HeapDirection.Increasing, comparer);

            if (values != null)
                foreach (var x in values)
                    cells[x] = heap.Enqueue(this.distances(x), x);

        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="comparer"></param>
        public FibonacciQueue(IDictionary<TVertex, TDistance> values, Func<TDistance, TDistance, int> comparer)
        {
            if (values is null)
                throw new ArgumentNullException(nameof(values));
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));

            distances = GetIndexer(values);
            cells = new Dictionary<TVertex, FibonacciHeapCell<TDistance, TVertex>>(values.Count);
            heap = new FibonacciHeap<TDistance, TVertex>(HeapDirection.Increasing, comparer);

            if (values != null)
                foreach (var kv in values)
                    cells[kv.Key] = heap.Enqueue(kv.Value, kv.Key);
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="values"></param>
        public FibonacciQueue(IDictionary<TVertex, TDistance> values) :
            this(values, Comparer<TDistance>.Default.Compare)
        {
            if (values is null)
                throw new ArgumentNullException(nameof(values));
        }

        #region IQueue<TVertex> Members

        public int Count
        {
            get { return heap.Count; }
        }

        public bool Contains(TVertex value)
        {
            return cells.TryGetValue(value, out var result) && !result.Removed;
        }

        public void Update(TVertex v)
        {
            heap.ChangeKey(cells[v], distances(v));
        }

        public void Enqueue(TVertex value)
        {
            cells[value] = heap.Enqueue(distances(value), value);
        }

        public TVertex Dequeue()
        {
            var result = heap.Top;
            heap.Dequeue();
            return result.Value;
        }

        public TVertex Peek()
        {
            return heap.Top.Value;
        }

        public TVertex[] ToArray()
        {
            var result = new TVertex[heap.Count];
            int i = 0;
            foreach (var entry in heap)
                result[i++] = entry.Value;
            return result;
        }

        #endregion

    }

}
