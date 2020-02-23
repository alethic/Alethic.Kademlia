namespace Cogito.Kademlia.Core
{

    /// <summary>
    /// Potentially holds a single value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public readonly struct Maybe<T>
    {

        /// <summary>
        /// Returns the maybe monad for any specific value.
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator Maybe<T>(T value)
        {
            return Create(value);
        }

        /// <summary>
        /// Gets a value result without any value.
        /// </summary>
        public static Maybe<T> Empty => new Maybe<T>(false, default);

        /// <summary>
        /// Gets a value result with the specified value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Maybe<T> Create(T value) => new Maybe<T>(true, value);

        readonly bool hasValue;
        readonly T value;

        /// <summary>
        /// Initailizes a new instance.
        /// </summary>
        public Maybe(bool hasValue, T value)
        {
            this.hasValue = hasValue;
            this.value = value;
        }

        /// <summary>
        /// Returns <c>true</c> if this object has a value.
        /// </summary>
        public bool HasValue => hasValue;

        /// <summary>
        /// Returns the result.
        /// </summary>
        public T Value => value;

    }

}
