using System;
using System.Collections.Generic;

namespace Cogito.Kademlia.Core
{

    /// <summary>
    /// Provides a comparer that actually chains to another comparator for a modification of the type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="V"></typeparam>
    class FuncComparer<T, V> : IComparer<T>
    {

        readonly Func<T, V> func;
        readonly IComparer<V> next;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="func"></param>
        /// <param name="next"></param>
        public FuncComparer(Func<T, V> func, IComparer<V> next)
        {
            this.func = func ?? throw new ArgumentNullException(nameof(func));
            this.next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public int Compare(T x, T y)
        {
            return next.Compare(func(x), func(y));
        }

    }

}
