using System.Collections.Generic;

namespace Cogito.Kademlia.Core
{

    public sealed class FibonacciHeapCell<TPriority, TValue>
    {

        bool marked;
        int degree;
        TPriority priority;
        TValue value;
        bool removed;
        readonly FibonacciHeapLinkedList<TPriority, TValue> children;
        FibonacciHeapCell<TPriority, TValue> parent;
        FibonacciHeapCell<TPriority, TValue> next;
        FibonacciHeapCell<TPriority, TValue> previous;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public FibonacciHeapCell()
        {
            children = new FibonacciHeapLinkedList<TPriority, TValue>();
        }

        /// <summary>
        /// Determines of a Node has had a child cut from it before
        /// </summary>
        public bool Marked
        {
            get { return marked; }
            set { marked = value; }
        }

        /// <summary>
        /// Determines the depth of a node
        /// </summary>
        public int Degree
        {
            get { return degree; }
            set { degree = value; }
        }

        public TPriority Priority
        {
            get { return priority; }
            set { priority = value; }
        }

        public TValue Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public bool Removed
        {
            get { return removed; }
            set { removed = value; }
        }

        public FibonacciHeapLinkedList<TPriority, TValue> Children
        {
            get { return children; }
        }

        public FibonacciHeapCell<TPriority, TValue> Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        public FibonacciHeapCell<TPriority, TValue> Next
        {
            get { return next; }
            set { next = value; }
        }

        public FibonacciHeapCell<TPriority, TValue> Previous
        {
            get { return previous; }
            set { previous = value; }
        }

        public KeyValuePair<TPriority, TValue> ToKeyValuePair()
        {
            return new KeyValuePair<TPriority, TValue>(Priority, Value);
        }

    }

}
