using System;
using System.Buffers;

namespace Alethic.Kademlia.Core
{

    public static class ReadOnlySequenceExtensions
    {

        /// <summary>
        /// A func-like delegate that accepts a read-only span.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="span"></param>
        /// <returns></returns>
        public delegate R ReadOnlySpanFunc<T, R>(ReadOnlySpan<T> span);

        /// <summary>
        /// A func-like delegate that accepts a read-only memory.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="span"></param>
        /// <returns></returns>
        public delegate R ReadOnlyMemoryFunc<T, R>(ReadOnlyMemory<T> span);

        /// <summary>
        /// Executes the given function with a slice of the sequence.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="self"></param>
        /// <param name="length"></param>
        /// <param name="with"></param>
        /// <returns></returns>
        public static R AdvanceOver<T, R>(this ref ReadOnlySequence<T> self, int length, ReadOnlySpanFunc<T, R> with)
            where T : unmanaged
        {
            var r = self.Slice(0, length);
            self = self.Slice(length);
            if (r.IsSingleSegment)
                return with(r.First.Span);
            else
            {
                var b = (Span<T>)stackalloc T[length];
                r.CopyTo(b);
                return with(b);
            }
        }

        /// <summary>
        /// Executes the given function with a slice of the sequence.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="self"></param>
        /// <param name="length"></param>
        /// <param name="with"></param>
        /// <returns></returns>
        public static R AdvanceOver<T, R>(this ref ReadOnlySequence<T> self, int length, ReadOnlyMemoryFunc<T, R> with)
        {
            var r = self.Slice(0, length);
            self = self.Slice(length);
            if (r.IsSingleSegment)
                return with(r.First);
            else
            {
                var b = (Memory<T>)new T[length];
                r.CopyTo(b.Span);
                return with(b);
            }
        }

    }

}
