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
            /// Evaluates to true at compile time if Armv8.1 Crypto intrinsics (AES, SHA1, SHA2, CRC32) are supported.
            /// </summary>
            public static bool IsNeonCryptoSupported { get { return false; } }

            /// <summary>SHA1 hash update (choose).
            /// <br/>Equivalent instruction: <c>SHA1C Qd,Sn,Vm.4S</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <param name="a1">UInt32 a1</param>
            /// <param name="a2">128-bit vector a2</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vsha1cq_u32(v128 a0, UInt32 a1, v128 a2)
            {
                throw new NotImplementedException();
            }

            /// <summary>SHA1 hash update (parity).
            /// <br/>Equivalent instruction: <c>SHA1P Qd,Sn,Vm.4S</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <param name="a1">UInt32 a1</param>
            /// <param name="a2">128-bit vector a2</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vsha1pq_u32(v128 a0, UInt32 a1, v128 a2)
            {
                throw new NotImplementedException();
            }

            /// <summary>SHA1 hash update (majority).
            /// <br/>Equivalent instruction: <c>SHA1M Qd,Sn,Vm.4S</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <param name="a1">UInt32 a1</param>
            /// <param name="a2">128-bit vector a2</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vsha1mq_u32(v128 a0, UInt32 a1, v128 a2)
            {
                throw new NotImplementedException();
            }

            /// <summary>SHA1 fixed rotate.
            /// <br/>Equivalent instruction: <c>SHA1H Sd,Sn</c></summary>
            /// <param name="a0">UInt32 a0</param>
            /// <returns>UInt32</returns>
            [DebuggerStepThrough]
            public static UInt32 vsha1h_u32(UInt32 a0)
            {
                throw new NotImplementedException();
            }

            /// <summary>SHA1 schedule update 0.
            /// <br/>Equivalent instruction: <c>SHA1SU0 Vd.4S,Vn.4S,Vm.4S</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <param name="a1">128-bit vector a1</param>
            /// <param name="a2">128-bit vector a2</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vsha1su0q_u32(v128 a0, v128 a1, v128 a2)
            {
                throw new NotImplementedException();
            }

            /// <summary>SHA1 schedule update 1.
            /// <br/>Equivalent instruction: <c>SHA1SU1 Vd.4S,Vn.4S</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <param name="a1">128-bit vector a1</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vsha1su1q_u32(v128 a0, v128 a1)
            {
                throw new NotImplementedException();
            }

            /// <summary>SHA256 hash update (part 1).
            /// <br/>Equivalent instruction: <c>SHA256H Qd,Qn,Vm.4S</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <param name="a1">128-bit vector a1</param>
            /// <param name="a2">128-bit vector a2</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vsha256hq_u32(v128 a0, v128 a1, v128 a2)
            {
                throw new NotImplementedException();
            }

            /// <summary>SHA256 hash update (part 2).
            /// <br/>Equivalent instruction: <c>SHA256H2 Qd,Qn,Vm.4S</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <param name="a1">128-bit vector a1</param>
            /// <param name="a2">128-bit vector a2</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vsha256h2q_u32(v128 a0, v128 a1, v128 a2)
            {
                throw new NotImplementedException();
            }

            /// <summary>SHA256 schedule update 0.
            /// <br/>Equivalent instruction: <c>SHA256SU0 Vd.4S,Vn.4S</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <param name="a1">128-bit vector a1</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vsha256su0q_u32(v128 a0, v128 a1)
            {
                throw new NotImplementedException();
            }

            /// <summary>SHA256 schedule update 1.
            /// <br/>Equivalent instruction: <c>SHA256SU1 Vd.4S,Vn.4S,Vm.4S</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <param name="a1">128-bit vector a1</param>
            /// <param name="a2">128-bit vector a2</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vsha256su1q_u32(v128 a0, v128 a1, v128 a2)
            {
                throw new NotImplementedException();
            }

            /// <summary>CRC32 checksum performs a cyclic redundancy check (CRC) calculation on a value held in a general-purpose register. It takes an input CRC value in the first source operand, performs a CRC on the input value in the second source operand, and returns the output CRC value. The second source operand can be 8, 16, 32, or 64 bits. To align with common usage, the bit order of the values is reversed as part of the operation, and the polynomial 0x04C11DB7 is used for the CRC calculation.
            /// <br/>Equivalent instruction: <c>CRC32B Wd,Wn,Wm</c></summary>
            /// <param name="a0">UInt32 a0</param>
            /// <param name="a1">Byte a1</param>
            /// <returns>UInt32</returns>
            [DebuggerStepThrough]
            public static UInt32 __crc32b(UInt32 a0, Byte a1)
            {
                throw new NotImplementedException();
            }

            /// <summary>CRC32 checksum performs a cyclic redundancy check (CRC) calculation on a value held in a general-purpose register. It takes an input CRC value in the first source operand, performs a CRC on the input value in the second source operand, and returns the output CRC value. The second source operand can be 8, 16, 32, or 64 bits. To align with common usage, the bit order of the values is reversed as part of the operation, and the polynomial 0x04C11DB7 is used for the CRC calculation.
            /// <br/>Equivalent instruction: <c>CRC32H Wd,Wn,Wm</c></summary>
            /// <param name="a0">UInt32 a0</param>
            /// <param name="a1">UInt16 a1</param>
            /// <returns>UInt32</returns>
            [DebuggerStepThrough]
            public static UInt32 __crc32h(UInt32 a0, UInt16 a1)
            {
                throw new NotImplementedException();
            }

            /// <summary>CRC32 checksum performs a cyclic redundancy check (CRC) calculation on a value held in a general-purpose register. It takes an input CRC value in the first source operand, performs a CRC on the input value in the second source operand, and returns the output CRC value. The second source operand can be 8, 16, 32, or 64 bits. To align with common usage, the bit order of the values is reversed as part of the operation, and the polynomial 0x04C11DB7 is used for the CRC calculation.
            /// <br/>Equivalent instruction: <c>CRC32W Wd,Wn,Wm</c></summary>
            /// <param name="a0">UInt32 a0</param>
            /// <param name="a1">UInt32 a1</param>
            /// <returns>UInt32</returns>
            [DebuggerStepThrough]
            public static UInt32 __crc32w(UInt32 a0, UInt32 a1)
            {
                throw new NotImplementedException();
            }

            /// <summary>CRC32 checksum performs a cyclic redundancy check (CRC) calculation on a value held in a general-purpose register. It takes an input CRC value in the first source operand, performs a CRC on the input value in the second source operand, and returns the output CRC value. The second source operand can be 8, 16, 32, or 64 bits. To align with common usage, the bit order of the values is reversed as part of the operation, and the polynomial 0x04C11DB7 is used for the CRC calculation.
            /// <br/>Equivalent instruction: <c>CRC32X Wd,Wn,Xm</c></summary>
            /// <param name="a0">UInt32 a0</param>
            /// <param name="a1">UInt64 a1</param>
            /// <returns>UInt32</returns>
            [DebuggerStepThrough]
            public static UInt32 __crc32d(UInt32 a0, UInt64 a1)
            {
                throw new NotImplementedException();
            }

            /// <summary>CRC32 checksum performs a cyclic redundancy check (CRC) calculation on a value held in a general-purpose register. It takes an input CRC value in the first source operand, performs a CRC on the input value in the second source operand, and returns the output CRC value. The second source operand can be 8, 16, 32, or 64 bits. To align with common usage, the bit order of the values is reversed as part of the operation, and the polynomial 0x04C11DB7 is used for the CRC calculation.
            /// <br/>Equivalent instruction: <c>CRC32CB Wd,Wn,Wm</c></summary>
            /// <param name="a0">UInt32 a0</param>
            /// <param name="a1">Byte a1</param>
            /// <returns>UInt32</returns>
            [DebuggerStepThrough]
            public static UInt32 __crc32cb(UInt32 a0, Byte a1)
            {
                throw new NotImplementedException();
            }

            /// <summary>CRC32 checksum performs a cyclic redundancy check (CRC) calculation on a value held in a general-purpose register. It takes an input CRC value in the first source operand, performs a CRC on the input value in the second source operand, and returns the output CRC value. The second source operand can be 8, 16, 32, or 64 bits. To align with common usage, the bit order of the values is reversed as part of the operation, and the polynomial 0x04C11DB7 is used for the CRC calculation.
            /// <br/>Equivalent instruction: <c>CRC32CH Wd,Wn,Wm</c></summary>
            /// <param name="a0">UInt32 a0</param>
            /// <param name="a1">UInt16 a1</param>
            /// <returns>UInt32</returns>
            [DebuggerStepThrough]
            public static UInt32 __crc32ch(UInt32 a0, UInt16 a1)
            {
                throw new NotImplementedException();
            }

            /// <summary>CRC32 checksum performs a cyclic redundancy check (CRC) calculation on a value held in a general-purpose register. It takes an input CRC value in the first source operand, performs a CRC on the input value in the second source operand, and returns the output CRC value. The second source operand can be 8, 16, 32, or 64 bits. To align with common usage, the bit order of the values is reversed as part of the operation, and the polynomial 0x04C11DB7 is used for the CRC calculation.
            /// <br/>Equivalent instruction: <c>CRC32CW Wd,Wn,Wm</c></summary>
            /// <param name="a0">UInt32 a0</param>
            /// <param name="a1">UInt32 a1</param>
            /// <returns>UInt32</returns>
            [DebuggerStepThrough]
            public static UInt32 __crc32cw(UInt32 a0, UInt32 a1)
            {
                throw new NotImplementedException();
            }

            /// <summary>CRC32 checksum performs a cyclic redundancy check (CRC) calculation on a value held in a general-purpose register. It takes an input CRC value in the first source operand, performs a CRC on the input value in the second source operand, and returns the output CRC value. The second source operand can be 8, 16, 32, or 64 bits. To align with common usage, the bit order of the values is reversed as part of the operation, and the polynomial 0x04C11DB7 is used for the CRC calculation.
            /// <br/>Equivalent instruction: <c>CRC32CX Wd,Wn,Xm</c></summary>
            /// <param name="a0">UInt32 a0</param>
            /// <param name="a1">UInt64 a1</param>
            /// <returns>UInt32</returns>
            [DebuggerStepThrough]
            public static UInt32 __crc32cd(UInt32 a0, UInt64 a1)
            {
                throw new NotImplementedException();
            }

            /// <summary>AES single round encryption.
            /// <br/>Equivalent instruction: <c>AESE Vd.16B,Vn.16B</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <param name="a1">128-bit vector a1</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vaeseq_u8(v128 a0, v128 a1)
            {
                throw new NotImplementedException();
            }

            /// <summary>AES single round decryption.
            /// <br/>Equivalent instruction: <c>AESD Vd.16B,Vn.16B</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <param name="a1">128-bit vector a1</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vaesdq_u8(v128 a0, v128 a1)
            {
                throw new NotImplementedException();
            }

            /// <summary>AES mix columns.
            /// <br/>Equivalent instruction: <c>AESMC Vd.16B,Vn.16B</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vaesmcq_u8(v128 a0)
            {
                throw new NotImplementedException();
            }

            /// <summary>AES inverse mix columns.
            /// <br/>Equivalent instruction: <c>AESIMC Vd.16B,Vn.16B</c></summary>
            /// <param name="a0">128-bit vector a0</param>
            /// <returns>128-bit vector</returns>
            [DebuggerStepThrough]
            public static v128 vaesimcq_u8(v128 a0)
            {
                throw new NotImplementedException();
            }
        }
    }
}
