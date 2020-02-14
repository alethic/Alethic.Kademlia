namespace Cogito.Kademlia.Core
{

    /// <summary>
    /// Provides extensions against <see cref="byte"/> values.
    /// </summary>
    internal static class ByteExtensions
    {

        /// <summary>
        /// Calculates the number of trailing zeros in the given unsigned byte.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static int CountLeadingZeros(this byte n)
        {
            if (n == 0)
                return 8;

            return ((ushort)n).CountLeadingZeros() - 8;
        }

    }

}
