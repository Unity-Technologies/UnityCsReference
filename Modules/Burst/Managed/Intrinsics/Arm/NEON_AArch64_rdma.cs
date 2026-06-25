// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;

namespace Unity.Burst.Intrinsics
{
    public unsafe static partial class Arm
    {
        public unsafe partial class Neon
        {
            /// <summary>
            /// Evaluates to true at compile time if Armv8.1 Rounding Double Multiply Add/Subtract intrinsics are supported.
            /// </summary>
            public static bool IsNeonRDMASupported { get { return false; } }

            /// <summary>Signed Saturating Rounding Doubling Multiply Accumulate returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and accumulates the most significant half of the final results with the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLAH Vd.4H,Vn.4H,Vm.4H</c></summary>
            /// <param name="a0">64-bit vector a0</param>
            /// <param name="a1">64-bit vector a1</param>
            /// <param name="a2">64-bit vector a2</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vqrdmlah_s16(v64 a0, v64 a1, v64 a2)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Accumulate returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and accumulates the most significant half of the final results with the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLAH Vd.2S,Vn.2S,Vm.2S</c></summary>
            /// <param name="a0">64-bit vector a0</param>
            /// <param name="a1">64-bit vector a1</param>
            /// <param name="a2">64-bit vector a2</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vqrdmlah_s32(v64 a0, v64 a1, v64 a2)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Accumulate returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and accumulates the most significant half of the final results with the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLAH Vd.8H,Vn.8H,Vm.8H</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <param name="a1">128-bit vector a1</param>
            /// <param name="a2">128-bit vector a2</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vqrdmlahq_s16(v128 a0, v128 a1, v128 a2)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Accumulate returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and accumulates the most significant half of the final results with the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLAH Vd.4S,Vn.4S,Vm.4S</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <param name="a1">128-bit vector a1</param>
            /// <param name="a2">128-bit vector a2</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vqrdmlahq_s32(v128 a0, v128 a1, v128 a2)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Subtract returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and subtracts the most significant half of the final results from the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLSH Vd.4H,Vn.4H,Vm.4H</c></summary>
            /// <param name="a0">64-bit vector a0</param>
            /// <param name="a1">64-bit vector a1</param>
            /// <param name="a2">64-bit vector a2</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vqrdmlsh_s16(v64 a0, v64 a1, v64 a2)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Subtract returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and subtracts the most significant half of the final results from the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLSH Vd.2S,Vn.2S,Vm.2S</c></summary>
            /// <param name="a0">64-bit vector a0</param>
            /// <param name="a1">64-bit vector a1</param>
            /// <param name="a2">64-bit vector a2</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vqrdmlsh_s32(v64 a0, v64 a1, v64 a2)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Subtract returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and subtracts the most significant half of the final results from the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLSH Vd.8H,Vn.8H,Vm.8H</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <param name="a1">128-bit vector a1</param>
            /// <param name="a2">128-bit vector a2</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vqrdmlshq_s16(v128 a0, v128 a1, v128 a2)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Subtract returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and subtracts the most significant half of the final results from the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLSH Vd.4S,Vn.4S,Vm.4S</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <param name="a1">128-bit vector a1</param>
            /// <param name="a2">128-bit vector a2</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vqrdmlshq_s32(v128 a0, v128 a1, v128 a2)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Accumulate returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and accumulates the most significant half of the final results with the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLAH Vd.4H,Vn.4H,Vm.H[lane]</c></summary>
            /// <param name="a0">64-bit vector a0</param>
            /// <param name="a1">64-bit vector a1</param>
            /// <param name="a2">64-bit vector a2</param>
            /// <param name="a3">Lane index to a2. Must be an immediate in the range of [0..3]</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vqrdmlah_lane_s16(v64 a0, v64 a1, v64 a2, Int32 a3)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Accumulate returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and accumulates the most significant half of the final results with the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLAH Vd.8H,Vn.8H,Vm.H[lane]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <param name="a1">128-bit vector a1</param>
            /// <param name="a2">64-bit vector a2</param>
            /// <param name="a3">Lane index to a2. Must be an immediate in the range of [0..3]</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vqrdmlahq_lane_s16(v128 a0, v128 a1, v64 a2, Int32 a3)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Accumulate returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and accumulates the most significant half of the final results with the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLAH Vd.4H,Vn.4H,Vm.H[lane]</c></summary>
            /// <param name="a0">64-bit vector a0</param>
            /// <param name="a1">64-bit vector a1</param>
            /// <param name="a2">128-bit vector a2</param>
            /// <param name="a3">Lane index to a2. Must be an immediate in the range of [0..7]</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vqrdmlah_laneq_s16(v64 a0, v64 a1, v128 a2, Int32 a3)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Accumulate returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and accumulates the most significant half of the final results with the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLAH Vd.8H,Vn.8H,Vm.H[lane]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <param name="a1">128-bit vector a1</param>
            /// <param name="a2">128-bit vector a2</param>
            /// <param name="a3">Lane index to a2. Must be an immediate in the range of [0..7]</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vqrdmlahq_laneq_s16(v128 a0, v128 a1, v128 a2, Int32 a3)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Accumulate returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and accumulates the most significant half of the final results with the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLAH Vd.2S,Vn.2S,Vm.S[lane]</c></summary>
            /// <param name="a0">64-bit vector a0</param>
            /// <param name="a1">64-bit vector a1</param>
            /// <param name="a2">64-bit vector a2</param>
            /// <param name="a3">Lane index to a2. Must be an immediate in the range of [0..1]</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vqrdmlah_lane_s32(v64 a0, v64 a1, v64 a2, Int32 a3)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Accumulate returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and accumulates the most significant half of the final results with the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLAH Vd.4S,Vn.4S,Vm.S[lane]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <param name="a1">128-bit vector a1</param>
            /// <param name="a2">64-bit vector a2</param>
            /// <param name="a3">Lane index to a2. Must be an immediate in the range of [0..1]</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vqrdmlahq_lane_s32(v128 a0, v128 a1, v64 a2, Int32 a3)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Accumulate returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and accumulates the most significant half of the final results with the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLAH Vd.2S,Vn.2S,Vm.S[lane]</c></summary>
            /// <param name="a0">64-bit vector a0</param>
            /// <param name="a1">64-bit vector a1</param>
            /// <param name="a2">128-bit vector a2</param>
            /// <param name="a3">Lane index to a2. Must be an immediate in the range of [0..3]</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vqrdmlah_laneq_s32(v64 a0, v64 a1, v128 a2, Int32 a3)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Accumulate returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and accumulates the most significant half of the final results with the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLAH Vd.4S,Vn.4S,Vm.S[lane]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <param name="a1">128-bit vector a1</param>
            /// <param name="a2">128-bit vector a2</param>
            /// <param name="a3">Lane index to a2. Must be an immediate in the range of [0..3]</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vqrdmlahq_laneq_s32(v128 a0, v128 a1, v128 a2, Int32 a3)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Subtract returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and subtracts the most significant half of the final results from the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLSH Vd.4H,Vn.4H,Vm.H[lane]</c></summary>
            /// <param name="a0">64-bit vector a0</param>
            /// <param name="a1">64-bit vector a1</param>
            /// <param name="a2">64-bit vector a2</param>
            /// <param name="a3">Lane index to a2. Must be an immediate in the range of [0..3]</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vqrdmlsh_lane_s16(v64 a0, v64 a1, v64 a2, Int32 a3)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Subtract returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and subtracts the most significant half of the final results from the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLSH Vd.8H,Vn.8H,Vm.H[lane]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <param name="a1">128-bit vector a1</param>
            /// <param name="a2">64-bit vector a2</param>
            /// <param name="a3">Lane index to a2. Must be an immediate in the range of [0..3]</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vqrdmlshq_lane_s16(v128 a0, v128 a1, v64 a2, Int32 a3)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Subtract returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and subtracts the most significant half of the final results from the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLSH Vd.4H,Vn.4H,Vm.H[lane]</c></summary>
            /// <param name="a0">64-bit vector a0</param>
            /// <param name="a1">64-bit vector a1</param>
            /// <param name="a2">128-bit vector a2</param>
            /// <param name="a3">Lane index to a2. Must be an immediate in the range of [0..7]</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vqrdmlsh_laneq_s16(v64 a0, v64 a1, v128 a2, Int32 a3)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Subtract returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and subtracts the most significant half of the final results from the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLSH Vd.8H,Vn.8H,Vm.H[lane]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <param name="a1">128-bit vector a1</param>
            /// <param name="a2">128-bit vector a2</param>
            /// <param name="a3">Lane index to a2. Must be an immediate in the range of [0..7]</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vqrdmlshq_laneq_s16(v128 a0, v128 a1, v128 a2, Int32 a3)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Subtract returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and subtracts the most significant half of the final results from the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLSH Vd.2S,Vn.2S,Vm.S[lane]</c></summary>
            /// <param name="a0">64-bit vector a0</param>
            /// <param name="a1">64-bit vector a1</param>
            /// <param name="a2">64-bit vector a2</param>
            /// <param name="a3">Lane index to a2. Must be an immediate in the range of [0..1]</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vqrdmlsh_lane_s32(v64 a0, v64 a1, v64 a2, Int32 a3)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Subtract returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and subtracts the most significant half of the final results from the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLSH Vd.4S,Vn.4S,Vm.S[lane]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <param name="a1">128-bit vector a1</param>
            /// <param name="a2">64-bit vector a2</param>
            /// <param name="a3">Lane index to a2. Must be an immediate in the range of [0..1]</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vqrdmlshq_lane_s32(v128 a0, v128 a1, v64 a2, Int32 a3)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Subtract returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and subtracts the most significant half of the final results from the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLSH Vd.2S,Vn.2S,Vm.S[lane]</c></summary>
            /// <param name="a0">64-bit vector a0</param>
            /// <param name="a1">64-bit vector a1</param>
            /// <param name="a2">128-bit vector a2</param>
            /// <param name="a3">Lane index to a2. Must be an immediate in the range of [0..3]</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vqrdmlsh_laneq_s32(v64 a0, v64 a1, v128 a2, Int32 a3)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Subtract returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and subtracts the most significant half of the final results from the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLSH Vd.4S,Vn.4S,Vm.S[lane]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <param name="a1">128-bit vector a1</param>
            /// <param name="a2">128-bit vector a2</param>
            /// <param name="a3">Lane index to a2. Must be an immediate in the range of [0..3]</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vqrdmlshq_laneq_s32(v128 a0, v128 a1, v128 a2, Int32 a3)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Accumulate returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and accumulates the most significant half of the final results with the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLAH Hd,Hn,Hm</c></summary>
            /// <param name="a0">Int16 a0</param>
            /// <param name="a1">Int16 a1</param>
            /// <param name="a2">Int16 a2</param>
            /// <returns>Int16</returns>
            [DebuggerStepThrough]
            public static Int16 vqrdmlahh_s16(Int16 a0, Int16 a1, Int16 a2)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Accumulate returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and accumulates the most significant half of the final results with the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLAH Sd,Sn,Sm</c></summary>
            /// <param name="a0">Int32 a0</param>
            /// <param name="a1">Int32 a1</param>
            /// <param name="a2">Int32 a2</param>
            /// <returns>Int32</returns>
            [DebuggerStepThrough]
            public static Int32 vqrdmlahs_s32(Int32 a0, Int32 a1, Int32 a2)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Subtract returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and subtracts the most significant half of the final results from the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLSH Hd,Hn,Hm</c></summary>
            /// <param name="a0">Int16 a0</param>
            /// <param name="a1">Int16 a1</param>
            /// <param name="a2">Int16 a2</param>
            /// <returns>Int16</returns>
            [DebuggerStepThrough]
            public static Int16 vqrdmlshh_s16(Int16 a0, Int16 a1, Int16 a2)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Subtract returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and subtracts the most significant half of the final results from the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLSH Sd,Sn,Sm</c></summary>
            /// <param name="a0">Int32 a0</param>
            /// <param name="a1">Int32 a1</param>
            /// <param name="a2">Int32 a2</param>
            /// <returns>Int32</returns>
            [DebuggerStepThrough]
            public static Int32 vqrdmlshs_s32(Int32 a0, Int32 a1, Int32 a2)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Accumulate returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and accumulates the most significant half of the final results with the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLAH Hd,Hn,Vm.H[lane]</c></summary>
            /// <param name="a0">Int16 a0</param>
            /// <param name="a1">Int16 a1</param>
            /// <param name="a2">64-bit vector a2</param>
            /// <param name="a3">Lane index to a2. Must be an immediate in the range of [0..3]</param>
            /// <returns>Int16</returns>
            [DebuggerStepThrough]
            public static Int16 vqrdmlahh_lane_s16(Int16 a0, Int16 a1, v64 a2, Int32 a3)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Accumulate returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and accumulates the most significant half of the final results with the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLAH Hd,Hn,Vm.H[lane]</c></summary>
            /// <param name="a0">Int16 a0</param>
            /// <param name="a1">Int16 a1</param>
            /// <param name="a2">128-bit vector a2</param>
            /// <param name="a3">Lane index to a2. Must be an immediate in the range of [0..7]</param>
            /// <returns>Int16</returns>
            [DebuggerStepThrough]
            public static Int16 vqrdmlahh_laneq_s16(Int16 a0, Int16 a1, v128 a2, Int32 a3)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Accumulate returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and accumulates the most significant half of the final results with the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLAH Sd,Sn,Vm.S[lane]</c></summary>
            /// <param name="a0">Int32 a0</param>
            /// <param name="a1">Int32 a1</param>
            /// <param name="a2">128-bit vector a2</param>
            /// <param name="a3">Lane index to a2. Must be an immediate in the range of [0..1]</param>
            /// <returns>Int32</returns>
            [DebuggerStepThrough]
            public static Int32 vqrdmlahs_lane_s32(Int32 a0, Int32 a1, v64 a2, Int32 a3)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Subtract returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and subtracts the most significant half of the final results from the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLSH Hd,Hn,Vm.H[lane]</c></summary>
            /// <param name="a0">Int16 a0</param>
            /// <param name="a1">Int16 a1</param>
            /// <param name="a2">64-bit vector a2</param>
            /// <param name="a3">Lane index to a2. Must be an immediate in the range of [0..3]</param>
            /// <returns>Int16</returns>
            [DebuggerStepThrough]
            public static Int16 vqrdmlshh_lane_s16(Int16 a0, Int16 a1, v64 a2, Int32 a3)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Subtract returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and subtracts the most significant half of the final results from the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLSH Hd,Hn,Vm.H[lane]</c></summary>
            /// <param name="a0">Int16 a0</param>
            /// <param name="a1">Int16 a1</param>
            /// <param name="a2">128-bit vector a2</param>
            /// <param name="a3">Lane index to a2. Must be an immediate in the range of [0..7]</param>
            /// <returns>Int16</returns>
            [DebuggerStepThrough]
            public static Int16 vqrdmlshh_laneq_s16(Int16 a0, Int16 a1, v128 a2, Int32 a3)
            {
                throw new NotImplementedException();
            }

            /// <summary>Signed Saturating Rounding Doubling Multiply Subtract returning High Half (by element). This instruction multiplies the vector elements of the first source SIMD&amp;FP register with the value of a vector element of the second source SIMD&amp;FP register without saturating the multiply results, doubles the results, and subtracts the most significant half of the final results from the vector elements of the destination SIMD&amp;FP register. The results are rounded.If any of the results overflow, they are saturated. The cumulative saturation bit, FPSR.QC, is set if saturation occurs.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>SQRDMLSH Sd,Sn,Vm.S[lane]</c></summary>
            /// <param name="a0">Int32 a0</param>
            /// <param name="a1">Int32 a1</param>
            /// <param name="a2">128-bit vector a2</param>
            /// <param name="a3">Lane index to a2. Must be an immediate in the range of [0..3]</param>
            /// <returns>Int32</returns>
            [DebuggerStepThrough]
            public static Int32 vqrdmlshs_lane_s32(Int32 a0, Int32 a1, v64 a2, Int32 a3)
            {
                throw new NotImplementedException();
            }
        }
    }
}
