// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics;

namespace Unity.Burst.Intrinsics
{
    public unsafe static partial class X86
    {
        /// <summary>
        /// bmi1 intrinsics
        /// </summary>
        public static class Bmi1
        {
            /// <summary>
            /// Evaluates to true at compile time if bmi1 intrinsics are supported.
            ///
            /// Burst ties bmi1 support to AVX2 support to simplify feature sets to support.
            /// </summary>
            public static bool IsBmi1Supported { get { return Avx2.IsAvx2Supported; } }

            /// <summary>
            /// Compute the bitwise NOT of 32-bit integer a and then AND with b, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** andn r32, r32, r32
            /// </remarks>
			/// <param name="a">32-bit integer</param>
			/// <param name="b">32-bit integer</param>
			/// <returns>32-bit integer</returns>
            [DebuggerStepThrough]
            public static uint andn_u32(uint a, uint b)
            {
                return ~a & b;
            }

            /// <summary>
            /// Compute the bitwise NOT of 64-bit integer a and then AND with b, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** andn r64, r64, r64
            /// </remarks>
			/// <param name="a">64-bit integer</param>
			/// <param name="b">64-bit integer</param>
			/// <returns>64-bit integer</returns>
            [DebuggerStepThrough]
            public static ulong andn_u64(ulong a, ulong b)
            {
                return ~a & b;
            }

            /// <summary>
            /// Extract contiguous bits from unsigned 32-bit integer a, and store the result in dst. Extract the number of bits specified by len, starting at the bit specified by start.
            /// </summary>
            /// <remarks>
            /// **** bextr r32, r32, r32
            /// </remarks>
			/// <param name="a">32-bit integer</param>
			/// <param name="start">Starting bit</param>
			/// <param name="len">Number of bits</param>
			/// <returns>32-bit integer</returns>
            [DebuggerStepThrough]
            public static uint bextr_u32(uint a, uint start, uint len)
            {
                start &= 0xff;

                if (start >= (sizeof(uint) * 8))
                {
                    return 0;
                }

                var aShifted = a >> (int)start;

                len &= 0xff;

                if (len >= (sizeof(uint) * 8))
                {
                    return aShifted;
                }

                return aShifted & ((1u << (int)len) - 1u);
            }

            /// <summary>
            /// Extract contiguous bits from unsigned 64-bit integer a, and store the result in dst. Extract the number of bits specified by len, starting at the bit specified by start.
            /// </summary>
            /// <remarks>
            /// **** bextr r64, r64, r64
            /// </remarks>
			/// <param name="a">64-bit integer</param>
			/// <param name="start">Starting bit</param>
			/// <param name="len">Number of bits</param>
			/// <returns>64-bit integer</returns>
            [DebuggerStepThrough]
            public static ulong bextr_u64(ulong a, uint start, uint len)
            {
                start &= 0xff;

                if (start >= (sizeof(ulong) * 8))
                {
                    return 0;
                }

                var aShifted = a >> (int)start;

                len &= 0xff;

                if (len >= (sizeof(ulong) * 8))
                {
                    return aShifted;
                }

                return aShifted & (((1ul) << (int)len) - 1u);
            }

            /// <summary>
            /// Extract contiguous bits from unsigned 32-bit integer a, and store the result in dst. Extract the number of bits specified by bits 15:8 of control, starting at the bit specified by bits 0:7 of control..
            /// </summary>
            /// <remarks>
            /// **** bextr r32, r32, r32
            /// </remarks>
			/// <param name="a">32-bit integer</param>
			/// <param name="control">Control</param>
			/// <returns>32-bit integer</returns>
            [DebuggerStepThrough]
            public static uint bextr2_u32(uint a, uint control)
            {
                uint start = control & byte.MaxValue;
                uint len = (control >> 8) & byte.MaxValue;
                return bextr_u32(a, start, len);
            }

            /// <summary>
            /// Extract contiguous bits from unsigned 64-bit integer a, and store the result in dst. Extract the number of bits specified by bits 15:8 of control, starting at the bit specified by bits 0:7 of control..
            /// </summary>
            /// <remarks>
            /// **** bextr r64, r64, r64
            /// </remarks>
			/// <param name="a">32-bit integer</param>
			/// <param name="control">Control</param>
			/// <returns>64-bit integer</returns>
            [DebuggerStepThrough]
            public static ulong bextr2_u64(ulong a, ulong control)
            {
                uint start = (uint)(control & byte.MaxValue);
                uint len = (uint)((control >> 8) & byte.MaxValue);
                return bextr_u64(a, start, len);
            }

            /// <summary>
            /// Extract the lowest set bit from unsigned 32-bit integer a and set the corresponding bit in dst. All other bits in dst are zeroed, and all bits are zeroed if no bits are set in a.
            /// </summary>
            /// <remarks>
            /// **** blsi r32, r32
            /// </remarks>
			/// <param name="a">32-bit integer</param>
			/// <returns>32-bit integer</returns>
            [DebuggerStepThrough]
            public static uint blsi_u32(uint a)
            {
                return (uint)(-(int)a) & a;
            }

            /// <summary>
            /// Extract the lowest set bit from unsigned 64-bit integer a and set the corresponding bit in dst. All other bits in dst are zeroed, and all bits are zeroed if no bits are set in a.
            /// </summary>
            /// <remarks>
            /// **** blsi r64, r64
            /// </remarks>
			/// <param name="a">64-bit integer</param>
			/// <returns>64-bit integer</returns>
            [DebuggerStepThrough]
            public static ulong blsi_u64(ulong a)
            {
                return (ulong)(-(long)a) & a;
            }
            /// <summary>
            /// Set all the lower bits of dst up to and including the lowest set bit in unsigned 32-bit integer a.
            /// </summary>
            /// <remarks>
            /// **** blsmsk r32, r32
            /// </remarks>
			/// <param name="a">32-bit integer</param>
			/// <returns>32-bit integer</returns>
            [DebuggerStepThrough]
            public static uint blsmsk_u32(uint a)
            {
                return (a - 1) ^ a;
            }

            /// <summary>
            /// Set all the lower bits of dst up to and including the lowest set bit in unsigned 64-bit integer a.
            /// </summary>
            /// <remarks>
            /// **** blsmsk r64, r64
            /// </remarks>
			/// <param name="a">64-bit integer</param>
			/// <returns>64-bit integer</returns>
            [DebuggerStepThrough]
            public static ulong blsmsk_u64(ulong a)
            {
                return (a - 1) ^ a;
            }

            /// <summary>
            /// Copy all bits from unsigned 32-bit integer a to dst, and reset (set to 0) the bit in dst that corresponds to the lowest set bit in a.
            /// </summary>
            /// <remarks>
            /// **** blsr r32, r32
            /// </remarks>
			/// <param name="a">32-bit integer</param>
			/// <returns>32-bit integer</returns>
            [DebuggerStepThrough]
            public static uint blsr_u32(uint a)
            {
                return (a - 1) & a;
            }

            /// <summary>
            /// Copy all bits from unsigned 64-bit integer a to dst, and reset (set to 0) the bit in dst that corresponds to the lowest set bit in a.
            /// </summary>
            /// <remarks>
            /// **** blsr r64, r64
            /// </remarks>
			/// <param name="a">64-bit integer</param>
			/// <returns>64-bit integer</returns>
            [DebuggerStepThrough]
            public static ulong blsr_u64(ulong a)
            {
                return (a - 1) & a;
            }

            /// <summary>
            /// Count the number of trailing zero bits in unsigned 32-bit integer a, and return that count in dst.
            /// </summary>
            /// <remarks>
            /// **** tzcnt r32, r32
            /// </remarks>
			/// <param name="a">32-bit integer</param>
			/// <returns>32-bit integer</returns>
            [DebuggerStepThrough]
            public static uint tzcnt_u32(uint a)
            {
                uint c = 32;
                a &= (uint)-(int)(a);
                if (a != 0) c--;
                if ((a & 0x0000FFFF) != 0) c -= 16;
                if ((a & 0x00FF00FF) != 0) c -= 8;
                if ((a & 0x0F0F0F0F) != 0) c -= 4;
                if ((a & 0x33333333) != 0) c -= 2;
                if ((a & 0x55555555) != 0) c -= 1;
                return c;
            }

            /// <summary>
            /// Count the number of trailing zero bits in unsigned 64-bit integer a, and return that count in dst.
            /// </summary>
            /// <remarks>
            /// **** tzcnt r64, r64
            /// </remarks>
			/// <param name="a">64-bit integer</param>
			/// <returns>64-bit integer</returns>
            [DebuggerStepThrough]
            public static ulong tzcnt_u64(ulong a)
            {
                ulong c = 64;
                a &= (ulong)-(long)(a);
                if (a != 0) c--;
                if ((a & 0x00000000FFFFFFFF) != 0) c -= 32;
                if ((a & 0x0000FFFF0000FFFF) != 0) c -= 16;
                if ((a & 0x00FF00FF00FF00FF) != 0) c -= 8;
                if ((a & 0x0F0F0F0F0F0F0F0F) != 0) c -= 4;
                if ((a & 0x3333333333333333) != 0) c -= 2;
                if ((a & 0x5555555555555555) != 0) c -= 1;
                return c;
            }
        }
    }
}
