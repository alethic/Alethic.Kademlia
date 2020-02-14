#if NETCOREAPP3_0
using System.Runtime.Intrinsics.X86;
#endif

namespace Cogito.Kademlia.Core
{

    /// <summary>
    /// Provides extensions against <see cref="uint"/> values.
    /// </summary>
    static class UInt32Extensions
    {

        /// <summary>
        /// Calculates the number of trailing zeros in the given unsigned integer.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static int CountLeadingZeros(this uint n)
        {
#if NETCOREAPP3_0
            if (Lzcnt.IsSupported)
                return (int)Lzcnt.LeadingZeroCount(n);
#endif

            if (n == 0)
                return 32;

            // do the smearing
            n |= n >> 1;
            n |= n >> 2;
            n |= n >> 4;
            n |= n >> 8;
            n |= n >> 16;

            // count the ones
            n -= n >> 1 & 0x55555555;
            n = (n >> 2 & 0x33333333) + (n & 0x33333333);
            n = (n >> 4) + n & 0x0f0f0f0f;
            n += n >> 8;
            n += n >> 16;
            return (int)(sizeof(uint) * 8 - (n & 0x0000003f));
        }

    }

}
