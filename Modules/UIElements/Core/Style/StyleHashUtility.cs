// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

namespace UnityEngine.UIElements.StyleSheets
{
    /// 64-bit FNV-1a hash folded to a 32-bit result, with two parallel lanes in
    /// the unrolled inner loop. Reads the input as a sequence of 8-byte chunks
    /// plus an optional trailing 4-byte chunk.
    internal static unsafe class StyleHashUtility
    {
        const ulong k_Prime = 1099511628211UL;       // 0x100000001B3
        const ulong k_OffsetBasis = 0xcbf29ce484222325UL;

        /// Hash `intCount` 4-byte chunks starting at `data` to a 32-bit value.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Hash32(void* data, int intCount)
        {
            unchecked
            {
                // Two parallel accumulators in the unrolled loop so the CPU can issue
                // both FNV mixes in the same cycle (no h-to-h dependency chain).
                //
                // h2 uses a distinct seed (~k_OffsetBasis) so the final h1 ^ h2 combine
                // doesn't collapse to 0 when both lanes evolve identically. The clearest
                // case is an all-zero input: with the same seed both lanes would compute
                // h = basis * prime^N forever and XOR to 0. Same applies to any input
                // where even/odd longs are pairwise equal. Distinct seeds break that
                // symmetry — multiplication by the FNV prime is bijective mod 2^64, so
                // h1[i] != h2[i] at every step.
                ulong h1 = k_OffsetBasis;
                ulong h2 = ~k_OffsetBasis;
                var lp = (ulong*)data;
                int longCount = intCount >> 1;
                int i = 0;
                int rounded = longCount & ~1;
                for (; i < rounded; i += 2)
                {
                    h1 = (h1 ^ lp[0]) * k_Prime;
                    h2 = (h2 ^ lp[1]) * k_Prime;
                    lp += 2;
                }
                ulong h = h1 ^ h2;
                for (; i < longCount; i++)
                    h = (h ^ *lp++) * k_Prime;
                if ((intCount & 1) != 0)
                    h = (h ^ ((uint*)data)[intCount - 1]) * k_Prime;
                return (int)(h ^ (h >> 32));
            }
        }
    }
}
