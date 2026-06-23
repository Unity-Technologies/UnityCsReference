// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics;

namespace Unity.Burst.Intrinsics
{
    public unsafe static partial class X86
    {
        /// <summary>
        /// bmi2 intrinsics
        /// </summary>
        public static class Bmi2
        {
            /// <summary>
            /// Evaluates to true at compile time if bmi2 intrinsics are supported.
            ///
            /// Burst ties bmi2 support to AVX2 support to simplify feature sets to support.
            /// </summary>
            public static bool IsBmi2Supported { get { return Avx2.IsAvx2Supported; } }

            /// <summary>
            /// Copy all bits from unsigned 32-bit integer a to dst, and reset (set to 0) the high bits in dst starting at index.
            /// </summary>
            /// <remarks>
            /// **** bzhi r32, r32, r32
            /// </remarks>
			/// <param name="a">32-bit integer</param>
			/// <param name="index">Starting point</param>
			/// <returns>32-bit integer</returns>
            [DebuggerStepThrough]
            public static uint bzhi_u32(uint a, uint index)
            {
                index &= 0xff;

                if (index >= (sizeof(uint) * 8))
                {
                    return a;
                }

                return a & ((1u << (int)index) - 1u);
            }

            /// <summary>
            /// Copy all bits from unsigned 64-bit integer a to dst, and reset (set to 0) the high bits in dst starting at index.
            /// </summary>
            /// <remarks>
            /// **** bzhi r64, r64, r64
            /// </remarks>
			/// <param name="a">64-bit integer</param>
			/// <param name="index">Starting point</param>
			/// <returns>64-bit integer</returns>
            [DebuggerStepThrough]
            public static ulong bzhi_u64(ulong a, ulong index)
            {
                index &= 0xff;

                if (index >= (sizeof(ulong) * 8))
                {
                    return a;
                }

                return a & ((1ul << (int)index) - 1ul);
            }

            /// <summary>
            /// Multiply unsigned 32-bit integers a and b, store the low 32-bits of the result in dst, and store the high 32-bits in hi. This does not read or write arithmetic flags.
            /// </summary>
            /// <remarks>
            /// **** mulx r32, r32, m32
            /// </remarks>
			/// <param name="a">32-bit integer</param>
			/// <param name="b">32-bit integer</param>
			/// <param name="hi">Stores the high 32-bits</param>
			/// <returns>32-bit integer</returns>
            [DebuggerStepThrough]
            public static uint mulx_u32(uint a, uint b, out uint hi)
            {
                ulong aBig = a;
                ulong bBig = b;
                ulong result = aBig * bBig;
                hi = (uint)(result >> 32);
                return (uint)(result & 0xffffffff);
            }

            /// <summary>
            /// Multiply unsigned 64-bit integers a and b, store the low 64-bits of the result in dst, and store the high 64-bits in hi. This does not read or write arithmetic flags.
            /// </summary>
            /// <remarks>
            /// **** mulx r64, r64, m64
            /// </remarks>
			/// <param name="a">64-bit integer</param>
			/// <param name="b">64-bit integer</param>
			/// <param name="hi">Stores the high 64-bits</param>
			/// <returns>64-bit integer</returns>
            [DebuggerStepThrough]
            public static ulong mulx_u64(ulong a, ulong b, out ulong hi)
            {
                return Common.umul128(a, b, out hi);
            }

            /// <summary>
            /// Deposit contiguous low bits from unsigned 32-bit integer a to dst at the corresponding bit locations specified by mask; all other bits in dst are set to zero.
            /// </summary>
            /// <remarks>
            /// **** pdep r32, r32, r32
            /// </remarks>
			/// <param name="a">32-bit integer</param>
			/// <param name="mask">Mask</param>
			/// <returns>32-bit integer</returns>
            [DebuggerStepThrough]
            public static uint pdep_u32(uint a, uint mask)
            {
                uint result = 0;

                int k = 0;

                for (int i = 0; i < 32; i++)
                {
                    if ((mask & (1u << i)) != 0)
                    {
                        result |= ((a >> k) & 1u) << i;
                        k++;
                    }
                }

                return result;
            }

            /// <summary>
            /// Deposit contiguous low bits from unsigned 64-bit integer a to dst at the corresponding bit locations specified by mask; all other bits in dst are set to zero.
            /// </summary>
            /// <remarks>
            /// **** pdep r64, r64, r64
            /// </remarks>
			/// <param name="a">64-bit integer</param>
			/// <param name="mask">Mask</param>
			/// <returns>64-bit integer</returns>
            [DebuggerStepThrough]
            public static ulong pdep_u64(ulong a, ulong mask)
            {
                ulong result = 0;

                int k = 0;

                for (int i = 0; i < 64; i++)
                {
                    if ((mask & (1ul << i)) != 0)
                    {
                        result |= ((a >> k) & 1ul) << i;
                        k++;
                    }
                }

                return result;
            }

            /// <summary>
            /// Extract bits from unsigned 32-bit integer a at the corresponding bit locations specified by mask to contiguous low bits in dst; the remaining upper bits in dst are set to zero.
            /// </summary>
            /// <remarks>
            /// **** pext r32, r32, r32
            /// </remarks>
			/// <param name="a">32-bit integer</param>
			/// <param name="mask">Mask</param>
			/// <returns>32-bit integer</returns>
            [DebuggerStepThrough]
            public static uint pext_u32(uint a, uint mask)
            {
                uint result = 0;

                int k = 0;

                for (int i = 0; i < 32; i++)
                {
                    if ((mask & (1u << i)) != 0)
                    {
                        result |= ((a >> i) & 1u) << k;
                        k++;
                    }
                }

                return result;
            }

            /// <summary>
            /// Extract bits from unsigned 64-bit integer a at the corresponding bit locations specified by mask to contiguous low bits in dst; the remaining upper bits in dst are set to zero.
            /// </summary>
            /// <remarks>
            /// **** pext r64, r64, r64
            /// </remarks>
			/// <param name="a">64-bit integer</param>
			/// <param name="mask">Mask</param>
			/// <returns>64-bit integer</returns>
            [DebuggerStepThrough]
            public static ulong pext_u64(ulong a, ulong mask)
            {
                ulong result = 0;

                int k = 0;

                for (int i = 0; i < 64; i++)
                {
                    if ((mask & (1ul << i)) != 0)
                    {
                        result |= ((a >> i) & 1ul) << k;
                        k++;
                    }
                }

                return result;
            }
        }
    }
}
