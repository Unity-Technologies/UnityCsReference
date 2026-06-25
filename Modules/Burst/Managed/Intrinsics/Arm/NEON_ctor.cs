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
            /// <summary>Insert vector element from another vector element. This instruction copies the vector element of the source SIMD&amp;FP register to the specified vector element of the destination SIMD&amp;FP register.This instruction can insert data into individual elements within a SIMD&amp;FP register without clearing the remaining bits to zero.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>INS Vd.D[0],Xn</c></summary>
            /// <param name="a0">UInt64 a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vcreate_s8(UInt64 a0)
            {
                return new v64(a0);
            }

            /// <summary>Insert vector element from another vector element. This instruction copies the vector element of the source SIMD&amp;FP register to the specified vector element of the destination SIMD&amp;FP register.This instruction can insert data into individual elements within a SIMD&amp;FP register without clearing the remaining bits to zero.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>INS Vd.D[0],Xn</c></summary>
            /// <param name="a0">UInt64 a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vcreate_s16(UInt64 a0)
            {
                return new v64(a0);
            }

            /// <summary>Insert vector element from another vector element. This instruction copies the vector element of the source SIMD&amp;FP register to the specified vector element of the destination SIMD&amp;FP register.This instruction can insert data into individual elements within a SIMD&amp;FP register without clearing the remaining bits to zero.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>INS Vd.D[0],Xn</c></summary>
            /// <param name="a0">UInt64 a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vcreate_s32(UInt64 a0)
            {
                return new v64(a0);
            }

            /// <summary>Insert vector element from another vector element. This instruction copies the vector element of the source SIMD&amp;FP register to the specified vector element of the destination SIMD&amp;FP register.This instruction can insert data into individual elements within a SIMD&amp;FP register without clearing the remaining bits to zero.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>INS Vd.D[0],Xn</c></summary>
            /// <param name="a0">UInt64 a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vcreate_s64(UInt64 a0)
            {
                return new v64(a0);
            }

            /// <summary>Insert vector element from another vector element. This instruction copies the vector element of the source SIMD&amp;FP register to the specified vector element of the destination SIMD&amp;FP register.This instruction can insert data into individual elements within a SIMD&amp;FP register without clearing the remaining bits to zero.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>INS Vd.D[0],Xn</c></summary>
            /// <param name="a0">UInt64 a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vcreate_u8(UInt64 a0)
            {
                return new v64(a0);
            }

            /// <summary>Insert vector element from another vector element. This instruction copies the vector element of the source SIMD&amp;FP register to the specified vector element of the destination SIMD&amp;FP register.This instruction can insert data into individual elements within a SIMD&amp;FP register without clearing the remaining bits to zero.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>INS Vd.D[0],Xn</c></summary>
            /// <param name="a0">UInt64 a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vcreate_u16(UInt64 a0)
            {
                return new v64(a0);
            }

            /// <summary>Insert vector element from another vector element. This instruction copies the vector element of the source SIMD&amp;FP register to the specified vector element of the destination SIMD&amp;FP register.This instruction can insert data into individual elements within a SIMD&amp;FP register without clearing the remaining bits to zero.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>INS Vd.D[0],Xn</c></summary>
            /// <param name="a0">UInt64 a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vcreate_u32(UInt64 a0)
            {
                return new v64(a0);
            }

            /// <summary>Insert vector element from another vector element. This instruction copies the vector element of the source SIMD&amp;FP register to the specified vector element of the destination SIMD&amp;FP register.This instruction can insert data into individual elements within a SIMD&amp;FP register without clearing the remaining bits to zero.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>INS Vd.D[0],Xn</c></summary>
            /// <param name="a0">UInt64 a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vcreate_u64(UInt64 a0)
            {
                return new v64(a0);
            }

            /// <summary>Insert vector element from another vector element. This instruction copies the vector element of the source SIMD&amp;FP register to the specified vector element of the destination SIMD&amp;FP register.This instruction can insert data into individual elements within a SIMD&amp;FP register without clearing the remaining bits to zero.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>INS Vd.D[0],Xn</c></summary>
            /// <param name="a0">UInt64 a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vcreate_f16(UInt64 a0)
            {
                return new v64(a0);
            }

            /// <summary>Insert vector element from another vector element. This instruction copies the vector element of the source SIMD&amp;FP register to the specified vector element of the destination SIMD&amp;FP register.This instruction can insert data into individual elements within a SIMD&amp;FP register without clearing the remaining bits to zero.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>INS Vd.D[0],Xn</c></summary>
            /// <param name="a0">UInt64 a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vcreate_f32(UInt64 a0)
            {
                return new v64(a0);
            }

            /// <summary>Insert vector element from another vector element. This instruction copies the vector element of the source SIMD&amp;FP register to the specified vector element of the destination SIMD&amp;FP register.This instruction can insert data into individual elements within a SIMD&amp;FP register without clearing the remaining bits to zero.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>INS Vd.D[0],Xn</c></summary>
            /// <param name="a0">UInt64 a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vcreate_f64(UInt64 a0)
            {
                return new v64(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.8B,rn</c></summary>
            /// <param name="a0">SByte a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vdup_n_s8(SByte a0)
            {
                return new v64(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.16B,rn</c></summary>
            /// <param name="a0">SByte a0</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vdupq_n_s8(SByte a0)
            {
                return new v128(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.4H,rn</c></summary>
            /// <param name="a0">Int16 a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vdup_n_s16(Int16 a0)
            {
                return new v64(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.8H,rn</c></summary>
            /// <param name="a0">Int16 a0</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vdupq_n_s16(Int16 a0)
            {
                return new v128(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.2S,rn</c></summary>
            /// <param name="a0">Int32 a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vdup_n_s32(Int32 a0)
            {
                return new v64(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.4S,rn</c></summary>
            /// <param name="a0">Int32 a0</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vdupq_n_s32(Int32 a0)
            {
                return new v128(a0);
            }

            /// <summary>Insert vector element from another vector element. This instruction copies the vector element of the source SIMD&amp;FP register to the specified vector element of the destination SIMD&amp;FP register.This instruction can insert data into individual elements within a SIMD&amp;FP register without clearing the remaining bits to zero.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>INS Dd.D[0],xn</c></summary>
            /// <param name="a0">Int64 a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vdup_n_s64(Int64 a0)
            {
                return new v64(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.2D,rn</c></summary>
            /// <param name="a0">Int64 a0</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vdupq_n_s64(Int64 a0)
            {
                return new v128(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.8B,rn</c></summary>
            /// <param name="a0">Byte a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vdup_n_u8(Byte a0)
            {
                return new v64(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.16B,rn</c></summary>
            /// <param name="a0">Byte a0</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vdupq_n_u8(Byte a0)
            {
                return new v128(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.4H,rn</c></summary>
            /// <param name="a0">UInt16 a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vdup_n_u16(UInt16 a0)
            {
                return new v64(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.8H,rn</c></summary>
            /// <param name="a0">UInt16 a0</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vdupq_n_u16(UInt16 a0)
            {
                return new v128(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.2S,rn</c></summary>
            /// <param name="a0">UInt32 a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vdup_n_u32(UInt32 a0)
            {
                return new v64(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.4S,rn</c></summary>
            /// <param name="a0">UInt32 a0</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vdupq_n_u32(UInt32 a0)
            {
                return new v128(a0);
            }

            /// <summary>Insert vector element from another vector element. This instruction copies the vector element of the source SIMD&amp;FP register to the specified vector element of the destination SIMD&amp;FP register.This instruction can insert data into individual elements within a SIMD&amp;FP register without clearing the remaining bits to zero.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>INS Dd.D[0],xn</c></summary>
            /// <param name="a0">UInt64 a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vdup_n_u64(UInt64 a0)
            {
                return new v64(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.2D,rn</c></summary>
            /// <param name="a0">UInt64 a0</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vdupq_n_u64(UInt64 a0)
            {
                return new v128(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.2S,rn</c></summary>
            /// <param name="a0">Single a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vdup_n_f32(Single a0)
            {
                return new v64(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.4S,rn</c></summary>
            /// <param name="a0">Single a0</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vdupq_n_f32(Single a0)
            {
                return new v128(a0);
            }

            /// <summary>Insert vector element from another vector element. This instruction copies the vector element of the source SIMD&amp;FP register to the specified vector element of the destination SIMD&amp;FP register.This instruction can insert data into individual elements within a SIMD&amp;FP register without clearing the remaining bits to zero.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>INS Dd.D[0],xn</c></summary>
            /// <param name="a0">Double a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vdup_n_f64(Double a0)
            {
                return new v64(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.2D,rn</c></summary>
            /// <param name="a0">Double a0</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vdupq_n_f64(Double a0)
            {
                return new v128(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.8B,rn</c></summary>
            /// <param name="a0">SByte a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vmov_n_s8(SByte a0)
            {
                return new v64(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.16B,rn</c></summary>
            /// <param name="a0">SByte a0</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vmovq_n_s8(SByte a0)
            {
                return new v128(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.4H,rn</c></summary>
            /// <param name="a0">Int16 a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vmov_n_s16(Int16 a0)
            {
                return new v64(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.8H,rn</c></summary>
            /// <param name="a0">Int16 a0</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vmovq_n_s16(Int16 a0)
            {
                return new v128(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.2S,rn</c></summary>
            /// <param name="a0">Int32 a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vmov_n_s32(Int32 a0)
            {
                return new v64(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.4S,rn</c></summary>
            /// <param name="a0">Int32 a0</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vmovq_n_s32(Int32 a0)
            {
                return new v128(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,rn</c></summary>
            /// <param name="a0">Int64 a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vmov_n_s64(Int64 a0)
            {
                return new v64(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.2D,rn</c></summary>
            /// <param name="a0">Int64 a0</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vmovq_n_s64(Int64 a0)
            {
                return new v128(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.8B,rn</c></summary>
            /// <param name="a0">Byte a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vmov_n_u8(Byte a0)
            {
                return new v64(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.16B,rn</c></summary>
            /// <param name="a0">Byte a0</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vmovq_n_u8(Byte a0)
            {
                return new v128(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.4H,rn</c></summary>
            /// <param name="a0">UInt16 a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vmov_n_u16(UInt16 a0)
            {
                return new v64(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.8H,rn</c></summary>
            /// <param name="a0">UInt16 a0</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vmovq_n_u16(UInt16 a0)
            {
                return new v128(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.2S,rn</c></summary>
            /// <param name="a0">UInt32 a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vmov_n_u32(UInt32 a0)
            {
                return new v64(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.4S,rn</c></summary>
            /// <param name="a0">UInt32 a0</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vmovq_n_u32(UInt32 a0)
            {
                return new v128(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,rn</c></summary>
            /// <param name="a0">UInt64 a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vmov_n_u64(UInt64 a0)
            {
                return new v64(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.2D,rn</c></summary>
            /// <param name="a0">UInt64 a0</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vmovq_n_u64(UInt64 a0)
            {
                return new v128(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.2S,rn</c></summary>
            /// <param name="a0">Single a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vmov_n_f32(Single a0)
            {
                return new v64(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.4S,rn</c></summary>
            /// <param name="a0">Single a0</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vmovq_n_f32(Single a0)
            {
                return new v128(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,rn</c></summary>
            /// <param name="a0">Double a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vmov_n_f64(Double a0)
            {
                return new v64(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.2D,rn</c></summary>
            /// <param name="a0">Double a0</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vmovq_n_f64(Double a0)
            {
                return new v128(a0);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[0]</c></summary>
            /// <param name="a0">64-bit vector a0</param>
            /// <param name="a1">64-bit vector a1</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vcombine_s8(v64 a0, v64 a1)
            {
                return new v128(a0, a1);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[0]</c></summary>
            /// <param name="a0">64-bit vector a0</param>
            /// <param name="a1">64-bit vector a1</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vcombine_s16(v64 a0, v64 a1)
            {
                return new v128(a0, a1);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[0]</c></summary>
            /// <param name="a0">64-bit vector a0</param>
            /// <param name="a1">64-bit vector a1</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vcombine_s32(v64 a0, v64 a1)
            {
                return new v128(a0, a1);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[0]</c></summary>
            /// <param name="a0">64-bit vector a0</param>
            /// <param name="a1">64-bit vector a1</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vcombine_s64(v64 a0, v64 a1)
            {
                return new v128(a0, a1);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[0]</c></summary>
            /// <param name="a0">64-bit vector a0</param>
            /// <param name="a1">64-bit vector a1</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vcombine_u8(v64 a0, v64 a1)
            {
                return new v128(a0, a1);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[0]</c></summary>
            /// <param name="a0">64-bit vector a0</param>
            /// <param name="a1">64-bit vector a1</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vcombine_u16(v64 a0, v64 a1)
            {
                return new v128(a0, a1);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[0]</c></summary>
            /// <param name="a0">64-bit vector a0</param>
            /// <param name="a1">64-bit vector a1</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vcombine_u32(v64 a0, v64 a1)
            {
                return new v128(a0, a1);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[0]</c></summary>
            /// <param name="a0">64-bit vector a0</param>
            /// <param name="a1">64-bit vector a1</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vcombine_u64(v64 a0, v64 a1)
            {
                return new v128(a0, a1);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[0]</c></summary>
            /// <param name="a0">64-bit vector a0</param>
            /// <param name="a1">64-bit vector a1</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vcombine_f16(v64 a0, v64 a1)
            {
                return new v128(a0, a1);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[0]</c></summary>
            /// <param name="a0">64-bit vector a0</param>
            /// <param name="a1">64-bit vector a1</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vcombine_f32(v64 a0, v64 a1)
            {
                return new v128(a0, a1);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[0]</c></summary>
            /// <param name="a0">64-bit vector a0</param>
            /// <param name="a1">64-bit vector a1</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vcombine_f64(v64 a0, v64 a1)
            {
                return new v128(a0, a1);
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[1]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vget_high_s8(v128 a0)
            {
                return a0.Hi64;
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[1]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vget_high_s16(v128 a0)
            {
                return a0.Hi64;
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[1]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vget_high_s32(v128 a0)
            {
                return a0.Hi64;
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[1]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vget_high_s64(v128 a0)
            {
                return a0.Hi64;
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[1]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vget_high_u8(v128 a0)
            {
                return a0.Hi64;
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[1]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vget_high_u16(v128 a0)
            {
                return a0.Hi64;
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[1]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vget_high_u32(v128 a0)
            {
                return a0.Hi64;
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[1]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vget_high_u64(v128 a0)
            {
                return a0.Hi64;
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[1]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vget_high_f32(v128 a0)
            {
                return a0.Hi64;
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[1]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vget_high_f64(v128 a0)
            {
                return a0.Hi64;
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[0]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vget_low_s8(v128 a0)
            {
                return a0.Lo64;
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[0]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vget_low_s16(v128 a0)
            {
                return a0.Lo64;
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[0]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vget_low_s32(v128 a0)
            {
                return a0.Lo64;
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[0]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vget_low_s64(v128 a0)
            {
                return a0.Lo64;
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[0]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vget_low_u8(v128 a0)
            {
                return a0.Lo64;
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[0]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vget_low_u16(v128 a0)
            {
                return a0.Lo64;
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[0]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vget_low_u32(v128 a0)
            {
                return a0.Lo64;
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[0]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vget_low_u64(v128 a0)
            {
                return a0.Lo64;
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[0]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vget_low_f32(v128 a0)
            {
                return a0.Lo64;
            }

            /// <summary>Duplicate vector element to vector or scalar. This instruction duplicates the vector element at the specified element index in the source SIMD&amp;FP register into a scalar or each element in a vector, and writes the result to the destination SIMD&amp;FP register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>DUP Vd.1D,Vn.D[0]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vget_low_f64(v128 a0)
            {
                return a0.Lo64;
            }

            /// <summary>Load multiple single-element structures to a register. This instruction loads multiple single-element structures from memory and writes the result to a SIMD&amp;FP register.
            /// <br/>Equivalent instruction: <c>LD1 {Vt.8B},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to load from</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vld1_s8(SByte* a0)
            {
                return *(v64*)a0;
            }

            /// <summary>Load multiple single-element structures to a register. This instruction loads multiple single-element structures from memory and writes the result to a SIMD&amp;FP register.
            /// <br/>Equivalent instruction: <c>LD1 {Vt.16B},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to load from</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vld1q_s8(SByte* a0)
            {
                return *(v128*)a0;
            }

            /// <summary>Load multiple single-element structures to a register. This instruction loads multiple single-element structures from memory and writes the result to a SIMD&amp;FP register.
            /// <br/>Equivalent instruction: <c>LD1 {Vt.4H},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to load from</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vld1_s16(Int16* a0)
            {
                return *(v64*)a0;
            }

            /// <summary>Load multiple single-element structures to a register. This instruction loads multiple single-element structures from memory and writes the result to a SIMD&amp;FP register.
            /// <br/>Equivalent instruction: <c>LD1 {Vt.8H},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to load from</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vld1q_s16(Int16* a0)
            {
                return *(v128*)a0;
            }

            /// <summary>Load multiple single-element structures to a register. This instruction loads multiple single-element structures from memory and writes the result to a SIMD&amp;FP register.
            /// <br/>Equivalent instruction: <c>LD1 {Vt.2S},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to load from</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vld1_s32(Int32* a0)
            {
                return *(v64*)a0;
            }

            /// <summary>Load multiple single-element structures to a register. This instruction loads multiple single-element structures from memory and writes the result to a SIMD&amp;FP register.
            /// <br/>Equivalent instruction: <c>LD1 {Vt.4S},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to load from</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vld1q_s32(Int32* a0)
            {
                return *(v128*)a0;
            }

            /// <summary>Load multiple single-element structures to a register. This instruction loads multiple single-element structures from memory and writes the result to a SIMD&amp;FP register.
            /// <br/>Equivalent instruction: <c>LD1 {Vt.1D},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to load from</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vld1_s64(Int64* a0)
            {
                return *(v64*)a0;
            }

            /// <summary>Load multiple single-element structures to a register. This instruction loads multiple single-element structures from memory and writes the result to a SIMD&amp;FP register.
            /// <br/>Equivalent instruction: <c>LD1 {Vt.2D},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to load from</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vld1q_s64(Int64* a0)
            {
                return *(v128*)a0;
            }

            /// <summary>Load multiple single-element structures to a register. This instruction loads multiple single-element structures from memory and writes the result to a SIMD&amp;FP register.
            /// <br/>Equivalent instruction: <c>LD1 {Vt.8B},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to load from</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vld1_u8(Byte* a0)
            {
                return *(v64*)a0;
            }

            /// <summary>Load multiple single-element structures to a register. This instruction loads multiple single-element structures from memory and writes the result to a SIMD&amp;FP register.
            /// <br/>Equivalent instruction: <c>LD1 {Vt.16B},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to load from</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vld1q_u8(Byte* a0)
            {
                return *(v128*)a0;
            }

            /// <summary>Load multiple single-element structures to a register. This instruction loads multiple single-element structures from memory and writes the result to a SIMD&amp;FP register.
            /// <br/>Equivalent instruction: <c>LD1 {Vt.4H},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to load from</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vld1_u16(UInt16* a0)
            {
                return *(v64*)a0;
            }

            /// <summary>Load multiple single-element structures to a register. This instruction loads multiple single-element structures from memory and writes the result to a SIMD&amp;FP register.
            /// <br/>Equivalent instruction: <c>LD1 {Vt.8H},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to load from</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vld1q_u16(UInt16* a0)
            {
                return *(v128*)a0;
            }

            /// <summary>Load multiple single-element structures to a register. This instruction loads multiple single-element structures from memory and writes the result to a SIMD&amp;FP register.
            /// <br/>Equivalent instruction: <c>LD1 {Vt.2S},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to load from</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vld1_u32(UInt32* a0)
            {
                return *(v64*)a0;
            }

            /// <summary>Load multiple single-element structures to a register. This instruction loads multiple single-element structures from memory and writes the result to a SIMD&amp;FP register.
            /// <br/>Equivalent instruction: <c>LD1 {Vt.4S},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to load from</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vld1q_u32(UInt32* a0)
            {
                return *(v128*)a0;
            }

            /// <summary>Load multiple single-element structures to a register. This instruction loads multiple single-element structures from memory and writes the result to a SIMD&amp;FP register.
            /// <br/>Equivalent instruction: <c>LD1 {Vt.1D},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to load from</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vld1_u64(UInt64* a0)
            {
                return *(v64*)a0;
            }

            /// <summary>Load multiple single-element structures to a register. This instruction loads multiple single-element structures from memory and writes the result to a SIMD&amp;FP register.
            /// <br/>Equivalent instruction: <c>LD1 {Vt.2D},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to load from</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vld1q_u64(UInt64* a0)
            {
                return *(v128*)a0;
            }

            /// <summary>Load multiple single-element structures to a register. This instruction loads multiple single-element structures from memory and writes the result to a SIMD&amp;FP register.
            /// <br/>Equivalent instruction: <c>LD1 {Vt.2S},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to load from</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vld1_f32(Single* a0)
            {
                return *(v64*)a0;
            }

            /// <summary>Load multiple single-element structures to a register. This instruction loads multiple single-element structures from memory and writes the result to a SIMD&amp;FP register.
            /// <br/>Equivalent instruction: <c>LD1 {Vt.4S},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to load from</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vld1q_f32(Single* a0)
            {
                return *(v128*)a0;
            }

            /// <summary>Load multiple single-element structures to a register. This instruction loads multiple single-element structures from memory and writes the result to a SIMD&amp;FP register.
            /// <br/>Equivalent instruction: <c>LD1 {Vt.1D},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to load from</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vld1_f64(Double* a0)
            {
                return *(v64*)a0;
            }

            /// <summary>Load multiple single-element structures to a register. This instruction loads multiple single-element structures from memory and writes the result to a SIMD&amp;FP register.
            /// <br/>Equivalent instruction: <c>LD1 {Vt.2D},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to load from</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vld1q_f64(Double* a0)
            {
                return *(v128*)a0;
            }


            /// <summary>Store multiple single-element structures from one, two, three, or four registers. This instruction stores elements to memory from one, two, three, or four SIMD&amp;FP registers, without interleaving. Every element of each register is stored.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>ST1 {Vt.8B},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to store to</param>
            /// <param name="a1">64-bit vector a1</param>
            [DebuggerStepThrough]
            public static void vst1_s8(SByte* a0, v64 a1)
            {
                *(v64*) a0 = a1;
            }

            /// <summary>Store multiple single-element structures from one, two, three, or four registers. This instruction stores elements to memory from one, two, three, or four SIMD&amp;FP registers, without interleaving. Every element of each register is stored.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>ST1 {Vt.16B},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to store to</param>
            /// <param name="a1">128-bit vector a1</param>
            [DebuggerStepThrough]
            public static void vst1q_s8(SByte* a0, v128 a1)
            {
                *(v128*) a0 = a1;
            }

            /// <summary>Store multiple single-element structures from one, two, three, or four registers. This instruction stores elements to memory from one, two, three, or four SIMD&amp;FP registers, without interleaving. Every element of each register is stored.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>ST1 {Vt.4H},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to store to</param>
            /// <param name="a1">64-bit vector a1</param>
            [DebuggerStepThrough]
            public static void vst1_s16(Int16* a0, v64 a1)
            {
                *(v64*) a0 = a1;
            }

            /// <summary>Store multiple single-element structures from one, two, three, or four registers. This instruction stores elements to memory from one, two, three, or four SIMD&amp;FP registers, without interleaving. Every element of each register is stored.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>ST1 {Vt.8H},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to store to</param>
            /// <param name="a1">128-bit vector a1</param>
            [DebuggerStepThrough]
            public static void vst1q_s16(Int16* a0, v128 a1)
            {
                *(v128*) a0 = a1;
            }

            /// <summary>Store multiple single-element structures from one, two, three, or four registers. This instruction stores elements to memory from one, two, three, or four SIMD&amp;FP registers, without interleaving. Every element of each register is stored.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>ST1 {Vt.2S},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to store to</param>
            /// <param name="a1">64-bit vector a1</param>
            [DebuggerStepThrough]
            public static void vst1_s32(Int32* a0, v64 a1)
            {
                *(v64*) a0 = a1;
            }

            /// <summary>Store multiple single-element structures from one, two, three, or four registers. This instruction stores elements to memory from one, two, three, or four SIMD&amp;FP registers, without interleaving. Every element of each register is stored.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>ST1 {Vt.4S},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to store to</param>
            /// <param name="a1">128-bit vector a1</param>
            [DebuggerStepThrough]
            public static void vst1q_s32(Int32* a0, v128 a1)
            {
                *(v128*) a0 = a1;
            }

            /// <summary>Store multiple single-element structures from one, two, three, or four registers. This instruction stores elements to memory from one, two, three, or four SIMD&amp;FP registers, without interleaving. Every element of each register is stored.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>ST1 {Vt.1D},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to store to</param>
            /// <param name="a1">64-bit vector a1</param>
            [DebuggerStepThrough]
            public static void vst1_s64(Int64* a0, v64 a1)
            {
                *(v64*) a0 = a1;
            }

            /// <summary>Store multiple single-element structures from one, two, three, or four registers. This instruction stores elements to memory from one, two, three, or four SIMD&amp;FP registers, without interleaving. Every element of each register is stored.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>ST1 {Vt.2D},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to store to</param>
            /// <param name="a1">128-bit vector a1</param>
            [DebuggerStepThrough]
            public static void vst1q_s64(Int64* a0, v128 a1)
            {
                *(v128*) a0 = a1;
            }

            /// <summary>Store multiple single-element structures from one, two, three, or four registers. This instruction stores elements to memory from one, two, three, or four SIMD&amp;FP registers, without interleaving. Every element of each register is stored.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>ST1 {Vt.8B},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to store to</param>
            /// <param name="a1">64-bit vector a1</param>
            [DebuggerStepThrough]
            public static void vst1_u8(Byte* a0, v64 a1)
            {
                *(v64*) a0 = a1;
            }

            /// <summary>Store multiple single-element structures from one, two, three, or four registers. This instruction stores elements to memory from one, two, three, or four SIMD&amp;FP registers, without interleaving. Every element of each register is stored.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>ST1 {Vt.16B},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to store to</param>
            /// <param name="a1">128-bit vector a1</param>
            [DebuggerStepThrough]
            public static void vst1q_u8(Byte* a0, v128 a1)
            {
                *(v128*) a0 = a1;
            }

            /// <summary>Store multiple single-element structures from one, two, three, or four registers. This instruction stores elements to memory from one, two, three, or four SIMD&amp;FP registers, without interleaving. Every element of each register is stored.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>ST1 {Vt.4H},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to store to</param>
            /// <param name="a1">64-bit vector a1</param>
            [DebuggerStepThrough]
            public static void vst1_u16(UInt16* a0, v64 a1)
            {
                *(v64*) a0 = a1;
            }

            /// <summary>Store multiple single-element structures from one, two, three, or four registers. This instruction stores elements to memory from one, two, three, or four SIMD&amp;FP registers, without interleaving. Every element of each register is stored.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>ST1 {Vt.8H},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to store to</param>
            /// <param name="a1">128-bit vector a1</param>
            [DebuggerStepThrough]
            public static void vst1q_u16(UInt16* a0, v128 a1)
            {
                *(v128*) a0 = a1;
            }

            /// <summary>Store multiple single-element structures from one, two, three, or four registers. This instruction stores elements to memory from one, two, three, or four SIMD&amp;FP registers, without interleaving. Every element of each register is stored.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>ST1 {Vt.2S},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to store to</param>
            /// <param name="a1">64-bit vector a1</param>
            [DebuggerStepThrough]
            public static void vst1_u32(UInt32* a0, v64 a1)
            {
                *(v64*) a0 = a1;
            }

            /// <summary>Store multiple single-element structures from one, two, three, or four registers. This instruction stores elements to memory from one, two, three, or four SIMD&amp;FP registers, without interleaving. Every element of each register is stored.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>ST1 {Vt.4S},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to store to</param>
            /// <param name="a1">128-bit vector a1</param>
            [DebuggerStepThrough]
            public static void vst1q_u32(UInt32* a0, v128 a1)
            {
                *(v128*) a0 = a1;
            }

            /// <summary>Store multiple single-element structures from one, two, three, or four registers. This instruction stores elements to memory from one, two, three, or four SIMD&amp;FP registers, without interleaving. Every element of each register is stored.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>ST1 {Vt.1D},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to store to</param>
            /// <param name="a1">64-bit vector a1</param>
            [DebuggerStepThrough]
            public static void vst1_u64(UInt64* a0, v64 a1)
            {
                *(v64*) a0 = a1;
            }

            /// <summary>Store multiple single-element structures from one, two, three, or four registers. This instruction stores elements to memory from one, two, three, or four SIMD&amp;FP registers, without interleaving. Every element of each register is stored.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>ST1 {Vt.2D},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to store to</param>
            /// <param name="a1">128-bit vector a1</param>
            [DebuggerStepThrough]
            public static void vst1q_u64(UInt64* a0, v128 a1)
            {
                *(v128*) a0 = a1;
            }

            /// <summary>Store multiple single-element structures from one, two, three, or four registers. This instruction stores elements to memory from one, two, three, or four SIMD&amp;FP registers, without interleaving. Every element of each register is stored.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>ST1 {Vt.2S},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to store to</param>
            /// <param name="a1">64-bit vector a1</param>
            [DebuggerStepThrough]
            public static void vst1_f32(Single* a0, v64 a1)
            {
                *(v64*) a0 = a1;
            }

            /// <summary>Store multiple single-element structures from one, two, three, or four registers. This instruction stores elements to memory from one, two, three, or four SIMD&amp;FP registers, without interleaving. Every element of each register is stored.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>ST1 {Vt.4S},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to store to</param>
            /// <param name="a1">128-bit vector a1</param>
            [DebuggerStepThrough]
            public static void vst1q_f32(Single* a0, v128 a1)
            {
                *(v128*) a0 = a1;
            }

            /// <summary>Store multiple single-element structures from one, two, three, or four registers. This instruction stores elements to memory from one, two, three, or four SIMD&amp;FP registers, without interleaving. Every element of each register is stored.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>ST1 {Vt.1D},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to store to</param>
            /// <param name="a1">64-bit vector a1</param>
            [DebuggerStepThrough]
            public static void vst1_f64(Double* a0, v64 a1)
            {
                *(v64*) a0 = a1;
            }

            /// <summary>Store multiple single-element structures from one, two, three, or four registers. This instruction stores elements to memory from one, two, three, or four SIMD&amp;FP registers, without interleaving. Every element of each register is stored.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.
            /// <br/>Equivalent instruction: <c>ST1 {Vt.2D},[Xn]</c></summary>
            /// <param name="a0">Pointer to the address to store to</param>
            /// <param name="a1">128-bit vector a1</param>
            [DebuggerStepThrough]
            public static void vst1q_f64(Double* a0, v128 a1)
            {
                *(v128*) a0 = a1;
            }
        }
    }
}
