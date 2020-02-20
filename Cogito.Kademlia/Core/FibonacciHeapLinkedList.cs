using System;
using System.Collections;
using System.Collections.Generic;

namespace Cogito.Kademlia.Core
{

    public sealed class FibonacciHeapLinkedList<TPriority, TValue> :
        IEnumerable<FibonacciHeapCell<TPriority, TValue>>
    {

        FibonacciHeapCell<TPriority, TValue> first;
        FibonacciHeapCell<TPriority, TValue> last;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        internal FibonacciHeapLinkedList()
        {
            first = null;
            last = null;
        }

        /// <summary>
        /// Gets the first element in the list.
        /// </summary>
        public FibonacciHeapCell<TPriority, TValue> First => first;

        /// <summary>
        /// Gets the last element in the list.
        /// </summary>
        public FibonacciHeapCell<TPriority, TValue> Last => last;

        public void Merge(FibonacciHeapLinkedList<TPriority, TValue> list)
        {
            if (list is null)
                throw new ArgumentNullException(nameof(list));

            if (list.First != null)
            {
                if (last != null)
                    last.Next = list.First;

                list.First.Previous = last;
                last = list.Last;

                if (first == null)
                    first = list.First;
            }
        }

        public void AddLast(FibonacciHeapCell<TPriority, TValue> node)
        {
            if (node is null)
                throw new ArgumentNullException(nameof(node));

            if (last != null)
                last.Next = node;

            node.Previous = last;
            last = node;

            if (first == null)
                first = node;
        }

        public void Remove(FibonacciHeapCell<TPriority, TValue> node)
        {
            if (node is null)
                throw new ArgumentNullException(nameof(node));

            if (node.Previous != null)
                node.Previous.Next = node.Next;
            else if (first == node)
                first = node.Next;

            if (node.Next != null)
                node.Next.Previous = node.Previous;
            else if (last == node)
                last = node.Previous;

            node.Next = null;
            node.Previous = null;
        }

        public void Clear()
        {
            first = null;
            last = null;
        }

        #region IEnumerable<FibonacciHeapNode<T, K>> Members

        public IEnumerator<FibonacciHeapCell<TPriority, TValue>> GetEnumerator()
        {
            for (var current = first; current != null; current = current.Next)
                yield return current;
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

    }

}
