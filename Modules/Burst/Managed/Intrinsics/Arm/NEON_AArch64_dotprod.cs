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
            /// Evaluates to true at compile time if Armv8.2 Dot Product intrinsics are supported.
            /// </summary>
            public static bool IsNeonDotProdSupported { get { return false; } }

            /// <summary>Dot Product unsigned arithmetic (vector, by element). This instruction performs the dot product of the four 8-bit elements in each 32-bit element of the first source register with the four 8-bit elements of an indexed 32-bit element in the second source register, accumulating the result into the corresponding 32-bit element of the destination register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.In Armv8.2 and Armv8.3, this is an optional instruction. From Armv8.4 it is mandatory for all implementations to support it.ID_AA64ISAR0_EL1.DP indicates whether this instruction is supported.
            /// <br/>Equivalent instruction: <c>UDOT Vd.2S,Vn.8B,Vm.8B</c></summary>
            /// <param name="a0">64-bit vector a0</param>
            /// <param name="a1">64-bit vector a1</param>
            /// <param name="a2">64-bit vector a2</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vdot_u32(v64 a0, v64 a1, v64 a2)
            {
                throw new NotImplementedException();
            }

            /// <summary>Dot Product signed arithmetic (vector, by element). This instruction performs the dot product of the four 8-bit elements in each 32-bit element of the first source register with the four 8-bit elements of an indexed 32-bit element in the second source register, accumulating the result into the corresponding 32-bit element of the destination register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.In Armv8.2 and Armv8.3, this is an optional instruction. From Armv8.4 it is mandatory for all implementations to support it.ID_AA64ISAR0_EL1.DP indicates whether this instruction is supported.
            /// <br/>Equivalent instruction: <c>SDOT Vd.2S,Vn.8B,Vm.8B</c></summary>
            /// <param name="a0">64-bit vector a0</param>
            /// <param name="a1">64-bit vector a1</param>
            /// <param name="a2">64-bit vector a2</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vdot_s32(v64 a0, v64 a1, v64 a2)
            {
                throw new NotImplementedException();
            }

            /// <summary>Dot Product unsigned arithmetic (vector, by element). This instruction performs the dot product of the four 8-bit elements in each 32-bit element of the first source register with the four 8-bit elements of an indexed 32-bit element in the second source register, accumulating the result into the corresponding 32-bit element of the destination register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.In Armv8.2 and Armv8.3, this is an optional instruction. From Armv8.4 it is mandatory for all implementations to support it.ID_AA64ISAR0_EL1.DP indicates whether this instruction is supported.
            /// <br/>Equivalent instruction: <c>UDOT Vd.4S,Vn.16B,Vm.16B</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <param name="a1">128-bit vector a1</param>
            /// <param name="a2">128-bit vector a2</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vdotq_u32(v128 a0, v128 a1, v128 a2)
            {
                throw new NotImplementedException();
            }

            /// <summary>Dot Product signed arithmetic (vector, by element). This instruction performs the dot product of the four 8-bit elements in each 32-bit element of the first source register with the four 8-bit elements of an indexed 32-bit element in the second source register, accumulating the result into the corresponding 32-bit element of the destination register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.In Armv8.2 and Armv8.3, this is an optional instruction. From Armv8.4 it is mandatory for all implementations to support it.ID_AA64ISAR0_EL1.DP indicates whether this instruction is supported.
            /// <br/>Equivalent instruction: <c>SDOT Vd.4S,Vn.16B,Vm.16B</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <param name="a1">128-bit vector a1</param>
            /// <param name="a2">128-bit vector a2</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vdotq_s32(v128 a0, v128 a1, v128 a2)
            {
                throw new NotImplementedException();
            }

            /// <summary>Dot Product unsigned arithmetic (vector, by element). This instruction performs the dot product of the four 8-bit elements in each 32-bit element of the first source register with the four 8-bit elements of an indexed 32-bit element in the second source register, accumulating the result into the corresponding 32-bit element of the destination register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.In Armv8.2 and Armv8.3, this is an optional instruction. From Armv8.4 it is mandatory for all implementations to support it.ID_AA64ISAR0_EL1.DP indicates whether this instruction is supported.
            /// <br/>Equivalent instruction: <c>UDOT Vd.2S,Vn.8B,Vm.4B[lane]</c></summary>
            /// <param name="a0">64-bit vector a0</param>
            /// <param name="a1">64-bit vector a1</param>
            /// <param name="a2">64-bit vector a2</param>
            /// <param name="a3">Lane index to a2. Must be an immediate in the range of [0..1]</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vdot_lane_u32(v64 a0, v64 a1, v64 a2, Int32 a3)
            {
                throw new NotImplementedException();
            }

            /// <summary>Dot Product signed arithmetic (vector, by element). This instruction performs the dot product of the four 8-bit elements in each 32-bit element of the first source register with the four 8-bit elements of an indexed 32-bit element in the second source register, accumulating the result into the corresponding 32-bit element of the destination register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.In Armv8.2 and Armv8.3, this is an optional instruction. From Armv8.4 it is mandatory for all implementations to support it.ID_AA64ISAR0_EL1.DP indicates whether this instruction is supported.
            /// <br/>Equivalent instruction: <c>SDOT Vd.2S,Vn.8B,Vm.4B[lane]</c></summary>
            /// <param name="a0">64-bit vector a0</param>
            /// <param name="a1">64-bit vector a1</param>
            /// <param name="a2">64-bit vector a2</param>
            /// <param name="a3">Lane index to a2. Must be an immediate in the range of [0..1]</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vdot_lane_s32(v64 a0, v64 a1, v64 a2, Int32 a3)
            {
                throw new NotImplementedException();
            }

            /// <summary>Dot Product unsigned arithmetic (vector, by element). This instruction performs the dot product of the four 8-bit elements in each 32-bit element of the first source register with the four 8-bit elements of an indexed 32-bit element in the second source register, accumulating the result into the corresponding 32-bit element of the destination register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.In Armv8.2 and Armv8.3, this is an optional instruction. From Armv8.4 it is mandatory for all implementations to support it.ID_AA64ISAR0_EL1.DP indicates whether this instruction is supported.
            /// <br/>Equivalent instruction: <c>UDOT Vd.4S,Vn.16B,Vm.4B[lane]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <param name="a1">128-bit vector a1</param>
            /// <param name="a2">128-bit vector a2</param>
            /// <param name="a3">Lane index to a2. Must be an immediate in the range of [0..3]</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vdotq_laneq_u32(v128 a0, v128 a1, v128 a2, Int32 a3)
            {
                throw new NotImplementedException();
            }

            /// <summary>Dot Product signed arithmetic (vector, by element). This instruction performs the dot product of the four 8-bit elements in each 32-bit element of the first source register with the four 8-bit elements of an indexed 32-bit element in the second source register, accumulating the result into the corresponding 32-bit element of the destination register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.In Armv8.2 and Armv8.3, this is an optional instruction. From Armv8.4 it is mandatory for all implementations to support it.ID_AA64ISAR0_EL1.DP indicates whether this instruction is supported.
            /// <br/>Equivalent instruction: <c>SDOT Vd.4S,Vn.16B,Vm.4B[lane]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <param name="a1">128-bit vector a1</param>
            /// <param name="a2">128-bit vector a2</param>
            /// <param name="a3">Lane index to a2. Must be an immediate in the range of [0..3]</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vdotq_laneq_s32(v128 a0, v128 a1, v128 a2, Int32 a3)
            {
                throw new NotImplementedException();
            }

            /// <summary>Dot Product unsigned arithmetic (vector, by element). This instruction performs the dot product of the four 8-bit elements in each 32-bit element of the first source register with the four 8-bit elements of an indexed 32-bit element in the second source register, accumulating the result into the corresponding 32-bit element of the destination register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.In Armv8.2 and Armv8.3, this is an optional instruction. From Armv8.4 it is mandatory for all implementations to support it.ID_AA64ISAR0_EL1.DP indicates whether this instruction is supported.
            /// <br/>Equivalent instruction: <c>UDOT Vd.2S,Vn.8B,Vm.4B[lane]</c></summary>
            /// <param name="a0">64-bit vector a0</param>
            /// <param name="a1">64-bit vector a1</param>
            /// <param name="a2">128-bit vector a2</param>
            /// <param name="a3">Lane index to a2. Must be an immediate in the range of [0..3]</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vdot_laneq_u32(v64 a0, v64 a1, v128 a2, Int32 a3)
            {
                throw new NotImplementedException();
            }

            /// <summary>Dot Product signed arithmetic (vector, by element). This instruction performs the dot product of the four 8-bit elements in each 32-bit element of the first source register with the four 8-bit elements of an indexed 32-bit element in the second source register, accumulating the result into the corresponding 32-bit element of the destination register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.In Armv8.2 and Armv8.3, this is an optional instruction. From Armv8.4 it is mandatory for all implementations to support it.ID_AA64ISAR0_EL1.DP indicates whether this instruction is supported.
            /// <br/>Equivalent instruction: <c>SDOT Vd.2S,Vn.8B,Vm.4B[lane]</c></summary>
            /// <param name="a0">64-bit vector a0</param>
            /// <param name="a1">64-bit vector a1</param>
            /// <param name="a2">128-bit vector a2</param>
            /// <param name="a3">Lane index to a2. Must be an immediate in the range of [0..3]</param>
            /// <returns>64-bit vector</returns>
            [DebuggerStepThrough]
            public static v64 vdot_laneq_s32(v64 a0, v64 a1, v128 a2, Int32 a3)
            {
                throw new NotImplementedException();
            }

            /// <summary>Dot Product unsigned arithmetic (vector, by element). This instruction performs the dot product of the four 8-bit elements in each 32-bit element of the first source register with the four 8-bit elements of an indexed 32-bit element in the second source register, accumulating the result into the corresponding 32-bit element of the destination register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.In Armv8.2 and Armv8.3, this is an optional instruction. From Armv8.4 it is mandatory for all implementations to support it.ID_AA64ISAR0_EL1.DP indicates whether this instruction is supported.
            /// <br/>Equivalent instruction: <c>UDOT Vd.4S,Vn.16B,Vm.4B[lane]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <param name="a1">128-bit vector a1</param>
            /// <param name="a2">64-bit vector a2</param>
            /// <param name="a3">Lane index to a2. Must be an immediate in the range of [0..1]</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vdotq_lane_u32(v128 a0, v128 a1, v64 a2, Int32 a3)
            {
                throw new NotImplementedException();
            }

            /// <summary>Dot Product signed arithmetic (vector, by element). This instruction performs the dot product of the four 8-bit elements in each 32-bit element of the first source register with the four 8-bit elements of an indexed 32-bit element in the second source register, accumulating the result into the corresponding 32-bit element of the destination register.Depending on the settings in the CPACR_EL1, CPTR_EL2, and CPTR_EL3 registers, and the current Security state and Exception level, an attempt to execute the instruction might be trapped.In Armv8.2 and Armv8.3, this is an optional instruction. From Armv8.4 it is mandatory for all implementations to support it.ID_AA64ISAR0_EL1.DP indicates whether this instruction is supported.
            /// <br/>Equivalent instruction: <c>SDOT Vd.4S,Vn.16B,Vm.4B[lane]</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <param name="a1">128-bit vector a1</param>
            /// <param name="a2">64-bit vector a2</param>
            /// <param name="a3">Lane index to a2. Must be an immediate in the range of [0..1]</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vdotq_lane_s32(v128 a0, v128 a1, v64 a2, Int32 a3)
            {
                throw new NotImplementedException();
            }
        }
    }
}
