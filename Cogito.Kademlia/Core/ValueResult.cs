namespace Cogito.Kademlia.Core
{

    /// <summary>
    /// Potentially holds a single value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct ValueResult<T>
    {

        /// <summary>
        /// Gets a value result without any value.
        /// </summary>
        public static ValueResult<T> Empty => new ValueResult<T>(false, default);

        /// <summary>
        /// Gets a value result with the specified value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ValueResult<T> Create(T value) => new ValueResult<T>(true, value);

        readonly bool hasValue;
        readonly T value;

        /// <summary>
        /// Initailizes a new instance.
        /// </summary>
        public ValueResult(bool hasValue, T value)
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
