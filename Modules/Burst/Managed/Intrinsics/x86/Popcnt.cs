// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics;

namespace Unity.Burst.Intrinsics
{
    public unsafe static partial class X86
    {
        /// <summary>
        /// popcnt intrinsics
        /// </summary>
        public static class Popcnt
        {
            /// <summary>
            /// Evaluates to true at compile time if popcnt intrinsics are supported.
            ///
            /// Burst ties popcnt support to SSE4.2 support to simplify feature sets to support.
            /// </summary>
            public static bool IsPopcntSupported { get { return Sse4_2.IsSse42Supported; } }

            /// <summary>
            /// Count the number of bits set to 1 in unsigned 32-bit integer a, and return that count in dst.
            /// </summary>
            /// <remarks>
            /// **** popcnt r32, r32
            /// </remarks>
			/// <param name="v">Integer to be counted in</param>
			/// <returns>Count</returns>
            [DebuggerStepThrough]
            public static int popcnt_u32(uint v)
            {
                int result = 0;
                uint mask = 0x80000000u;
                while (mask != 0)
                {
                    result += ((v & mask) != 0) ? 1 : 0;
                    mask >>= 1;
                }
                return result;
            }

            /// <summary>
            /// Count the number of bits set to 1 in unsigned 64-bit integer a, and return that count in dst.
            /// </summary>
            /// <remarks>
            /// **** popcnt r64, r64
            /// </remarks>
			/// <param name="v">Integer to be counted in</param>
			/// <returns>Count</returns>
            [DebuggerStepThrough]
            public static int popcnt_u64(ulong v)
            {
                int result = 0;
                ulong mask = 0x8000000000000000u;
                while (mask != 0)
                {
                    result += ((v & mask) != 0) ? 1 : 0;
                    mask >>= 1;
                }
                return result;
            }
        }
    }
}
