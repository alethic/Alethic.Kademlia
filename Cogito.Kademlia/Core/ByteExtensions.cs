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

            var t = 0;

            for (var i = 0; i < 8; i++)
            {
                if (((n << i) & (1 << 7)) != 0)
                    break;

                t++;
            }

            return t;
        }

    }

}
