using System;
using System.Collections;
using System.Collections.Generic;

namespace Cogito.Kademlia.Core
{

    public sealed class FibonacciHeap<TPriority, TValue> :
        IEnumerable<KeyValuePair<TPriority, TValue>>
    {

        struct NodeLevel
        {

            public readonly FibonacciHeapCell<TPriority, TValue> node;
            public readonly int level;

            /// <summary>
            /// Initializes a new instance.
            /// </summary>
            /// <param name="node"></param>
            /// <param name="level"></param>
            public NodeLevel(FibonacciHeapCell<TPriority, TValue> node, int level)
            {
                this.node = node;
                this.level = level;
            }

        }

        readonly Func<TPriority, TPriority, int> comparer;
        readonly HeapDirection direction;
        readonly FibonacciHeapLinkedList<TPriority, TValue> nodes;
        readonly Dictionary<int, FibonacciHeapCell<TPriority, TValue>> degreeToNode;
        readonly short directionMultiplier;

        FibonacciHeapCell<TPriority, TValue> next;
        int count;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public FibonacciHeap() :
            this(HeapDirection.Increasing, Comparer<TPriority>.Default.Compare)
        {

        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="direction"></param>
        public FibonacciHeap(HeapDirection direction) :
            this(direction, Comparer<TPriority>.Default.Compare)
        {

        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="comparer"></param>
        public FibonacciHeap(HeapDirection direction, Func<TPriority, TPriority, int> comparer)
        {
            nodes = new FibonacciHeapLinkedList<TPriority, TValue>();
            degreeToNode = new Dictionary<int, FibonacciHeapCell<TPriority, TValue>>();
            directionMultiplier = (short)(direction == HeapDirection.Increasing ? 1 : -1);
            this.direction = direction;
            this.comparer = comparer;
            count = 0;
        }

        public int Count
        {
            get { return count; }
        }

        public HeapDirection Direction
        {
            get { return direction; }
        }

        public Func<TPriority, TPriority, int> Comparer
        {
            get { return comparer; }
        }

        public string DrawHeap()
        {
            var lines = new List<string>();
            var lineNum = 0;
            var columnPosition = 0;
            var list = new List<NodeLevel>();
            foreach (var node in nodes)
                list.Add(new NodeLevel(node, 0));
            list.Reverse();
            var stack = new Stack<NodeLevel>(list);
            while (stack.Count > 0)
            {
                var currentcell = stack.Pop();
                lineNum = currentcell.level;
                if (lines.Count <= lineNum)
                    lines.Add(string.Empty);
                var currentLine = lines[lineNum];
                currentLine = currentLine.PadRight(columnPosition, ' ');
                var nodeString = currentcell.node.Priority.ToString() + (currentcell.node.Marked ? "*" : "") + " ";
                currentLine += nodeString;
                if (currentcell.node.Children != null && currentcell.node.Children.First != null)
                {
                    var children = new List<FibonacciHeapCell<TPriority, TValue>>(currentcell.node.Children);
                    children.Reverse();
                    foreach (var child in children)
                        stack.Push(new NodeLevel(child, currentcell.level + 1));
                }
                else
                {
                    columnPosition += nodeString.Length;
                }
                lines[lineNum] = currentLine;
            }
            return string.Join(Environment.NewLine, lines.ToArray());
        }

        public FibonacciHeapCell<TPriority, TValue> Enqueue(TPriority priority, TValue value)
        {
            var newNode = new FibonacciHeapCell<TPriority, TValue>()
            {
                Priority = priority,
                Value = value,
                Marked = false,
                Degree = 1,
                Next = null,
                Previous = null,
                Parent = null,
                Removed = false
            };

            // we don't do any book keeping or maintenance of the heap on Enqueue,
            // we just add this node to the end of the list of Heaps, updating the Next if required
            nodes.AddLast(newNode);
            if (next == null || comparer(newNode.Priority, next.Priority) * directionMultiplier < 0)
                next = newNode;

            count++;

            return newNode;
        }

        public void Delete(FibonacciHeapCell<TPriority, TValue> node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            ChangeKeyInternal(node, default, true);
            Dequeue();
        }

        public void ChangeKey(FibonacciHeapCell<TPriority, TValue> node, TPriority newKey)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            ChangeKeyInternal(node, newKey, false);
        }

        void ChangeKeyInternal(FibonacciHeapCell<TPriority, TValue> node, TPriority newKey, bool deletingNode)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            var delta = System.Math.Sign(comparer(node.Priority, newKey));
            if (delta == 0)
                return;

            if (delta == directionMultiplier || deletingNode)
            {
                // new value is in the same direciton as the heap
                node.Priority = newKey;
                var parentNode = node.Parent;
                if (parentNode != null && (comparer(newKey, node.Parent.Priority) * directionMultiplier < 0 || deletingNode))
                {
                    node.Marked = false;
                    parentNode.Children.Remove(node);
                    UpdateNodesDegree(parentNode);
                    node.Parent = null;
                    nodes.AddLast(node);

                    // this loop is the cascading cut, we continue to cut
                    // ancestors of the node reduced until we hit a root 
                    // or we found an unmarked ancestor
                    while (parentNode.Marked && parentNode.Parent != null)
                    {
                        parentNode.Parent.Children.Remove(parentNode);
                        UpdateNodesDegree(parentNode);
                        parentNode.Marked = false;
                        nodes.AddLast(parentNode);
                        var currentParent = parentNode;
                        parentNode = parentNode.Parent;
                        currentParent.Parent = null;
                    }

                    if (parentNode.Parent != null)
                    {
                        // we mark this node to note that it's had a child
                        // cut from it before
                        parentNode.Marked = true;
                    }
                }

                // update next
                if (deletingNode || comparer(newKey, next.Priority) * directionMultiplier < 0)
                {
                    next = node;
                }
            }
            else
            {
                // new value is in opposite direction of Heap, cut all children violating heap condition
                node.Priority = newKey;
                if (node.Children != null)
                {
                    List<FibonacciHeapCell<TPriority, TValue>> toupdate = null;
                    foreach (var child in node.Children)
                    {
                        if (comparer(node.Priority, child.Priority) * directionMultiplier > 0)
                        {
                            if (toupdate == null)
                                toupdate = new List<FibonacciHeapCell<TPriority, TValue>>();
                            toupdate.Add(child);
                        }
                    }

                    if (toupdate != null)
                    {
                        foreach (var child in toupdate)
                        {
                            node.Marked = true;
                            node.Children.Remove(child);
                            child.Parent = null;
                            child.Marked = false;
                            nodes.AddLast(child);
                            UpdateNodesDegree(node);
                        }
                    }
                }

                UpdateNext();
            }
        }

        static int Max<T>(IEnumerable<T> values, Func<T, int> converter)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));

            var max = int.MinValue;

            foreach (var value in values)
            {
                var v = converter(value);
                if (max < v)
                    max = v;
            }

            return max;
        }

        /// <summary>
        /// Updates the degree of a node, cascading to update the degree of the parents if nessecary
        /// </summary>
        /// <param name="parentNode"></param>
        void UpdateNodesDegree(FibonacciHeapCell<TPriority, TValue> parentNode)
        {
            if (parentNode == null)
                throw new ArgumentNullException(nameof(parentNode));

            var oldDegree = parentNode.Degree;
            parentNode.Degree = parentNode.Children.First != null ? Max(parentNode.Children, x => x.Degree) + 1 : 1;
            if (oldDegree != parentNode.Degree)
            {
                if (degreeToNode.TryGetValue(oldDegree, out var degreeMapValue) && degreeMapValue == parentNode)
                {
                    degreeToNode.Remove(oldDegree);
                }
                else if (parentNode.Parent != null)
                {
                    UpdateNodesDegree(parentNode.Parent);
                }
            }
        }

        public KeyValuePair<TPriority, TValue> Dequeue()
        {
            if (count == 0)
                throw new InvalidOperationException();

            var result = new KeyValuePair<TPriority, TValue>(next.Priority, next.Value);

            nodes.Remove(next);
            next.Next = null;
            next.Parent = null;
            next.Previous = null;
            next.Removed = true;

            if (degreeToNode.TryGetValue(next.Degree, out var currentDegreeNode))
                if (currentDegreeNode == next)
                    degreeToNode.Remove(next.Degree);

            foreach (var child in next.Children)
                child.Parent = null;

            nodes.Merge(next.Children);
            next.Children.Clear();
            count--;
            UpdateNext();

            return result;
        }

        /// <summary>
        /// Updates the Next pointer, maintaining the heap by folding duplicate heap degrees into each other
        /// takes O(lg(N)) time amortized.
        /// </summary>
        void UpdateNext()
        {
            CompressHeap();
            var node = nodes.First;
            next = nodes.First;

            while (node != null)
            {
                if (comparer(node.Priority, next.Priority) * directionMultiplier < 0)
                    next = node;

                node = node.Next;
            }
        }

        void CompressHeap()
        {
            var node = nodes.First;

            while (node != null)
            {
                var nextNode = node.Next;

                while (degreeToNode.TryGetValue(node.Degree, out var currentDegreeNode) && currentDegreeNode != node)
                {
                    degreeToNode.Remove(node.Degree);

                    if (comparer(currentDegreeNode.Priority, node.Priority) * directionMultiplier <= 0)
                    {
                        if (node == nextNode)
                            nextNode = node.Next;

                        ReduceNodes(currentDegreeNode, node);
                        node = currentDegreeNode;
                    }
                    else
                    {
                        if (currentDegreeNode == nextNode)
                            nextNode = currentDegreeNode.Next;

                        ReduceNodes(node, currentDegreeNode);
                    }
                }

                degreeToNode[node.Degree] = node;
                node = nextNode;
            }
        }

        /// <summary>
        /// Given two nodes, adds the child node as a child of the parent node.
        /// </summary>
        /// <param name="parentNode"></param>
        /// <param name="childNode"></param>
        void ReduceNodes(FibonacciHeapCell<TPriority, TValue> parentNode, FibonacciHeapCell<TPriority, TValue> childNode)
        {
            if (parentNode is null)
                throw new ArgumentNullException(nameof(parentNode));
            if (childNode is null)
                throw new ArgumentNullException(nameof(childNode));

            nodes.Remove(childNode);
            parentNode.Children.AddLast(childNode);
            childNode.Parent = parentNode;
            childNode.Marked = false;
            if (parentNode.Degree == childNode.Degree)
                parentNode.Degree += 1;
        }

        public bool IsEmpty => nodes.First == null;

        public FibonacciHeapCell<TPriority, TValue> Top => next;

        public void Merge(FibonacciHeap<TPriority, TValue> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            if (other.Direction != Direction)
                throw new Exception("Error: Heaps must go in the same direction when merging");

            nodes.Merge(other.nodes);

            if (comparer(other.Top.Priority, next.Priority) * directionMultiplier < 0)
                next = other.next;

            count += other.Count;
        }

        public IEnumerator<KeyValuePair<TPriority, TValue>> GetEnumerator()
        {
            var tempHeap = new FibonacciHeap<TPriority, TValue>(Direction, comparer);
            var nodeStack = new Stack<FibonacciHeapCell<TPriority, TValue>>();

            foreach (var i in nodes)
                nodeStack.Push(i);

            while (nodeStack.Count > 0)
            {
                var topNode = nodeStack.Peek();
                tempHeap.Enqueue(topNode.Priority, topNode.Value);
                nodeStack.Pop();

                foreach (var x in topNode.Children)
                    nodeStack.Push(x);
            }

            while (!tempHeap.IsEmpty)
            {
                yield return tempHeap.Top.ToKeyValuePair();
                tempHeap.Dequeue();
            }
        }

        public IEnumerable<KeyValuePair<TPriority, TValue>> GetDestructiveEnumerator()
        {
            while (!IsEmpty)
            {
                yield return Top.ToKeyValuePair();
                Dequeue();
            }
        }

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

    }

}
