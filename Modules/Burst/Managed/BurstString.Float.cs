// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst.LowLevel.Unsafe;

namespace Unity.Burst
{
    internal static partial class BurstString
    {
        // This file provides an implementation for formatting floating point numbers that is compatible
        // with Burst

        // ------------------------------------------------------------------------------
        // Part of code translated to C# from http://www.ryanjuckett.com/programming/printing-floating-point-numbers
        // with the following license:
        /******************************************************************************
          Copyright (c) 2014 Ryan Juckett
          http://www.ryanjuckett.com/

          This software is provided 'as-is', without any express or implied
          warranty. In no event will the authors be held liable for any damages
          arising from the use of this software.

          Permission is granted to anyone to use this software for any purpose,
          including commercial applications, and to alter it and redistribute it
          freely, subject to the following restrictions:

          1. The origin of this software must not be misrepresented; you must not
             claim that you wrote the original software. If you use this software
             in a product, an acknowledgment in the product documentation would be
             appreciated but is not required.

          2. Altered source versions must be plainly marked as such, and must not be
             misrepresented as being the original software.

          3. This notice may not be removed or altered from any source
             distribution.
        ******************************************************************************/

        //******************************************************************************
        // Get the log base 2 of a 32-bit unsigned integer.
        // http://graphics.stanford.edu/~seander/bithacks.html#IntegerLogLookup
        //******************************************************************************
        private static readonly byte[] logTable = new byte[256]
        {
            0, 0, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3,
            4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
            5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
            5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
            6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
            6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
            6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
            6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7
        };

        private static uint LogBase2(uint val)
        {
            uint temp;
            temp = val >> 24;
            if (temp != 0)
                return (uint)(24 + logTable[(int)temp]);

            temp = val >> 16;
            if (temp != 0)
                return (uint)(16 + logTable[temp]);

            temp = val >> 8;
            if (temp != 0)
                return (uint)(8 + logTable[temp]);

            return logTable[val];
        }

        //******************************************************************************
        // This structure stores a high precision unsigned integer. It uses a buffer
        // of 32 bit integer blocks along with a length. The lowest bits of the integer
        // are stored at the start of the buffer and the length is set to the minimum
        // value that contains the integer. Thus, there are never any zero blocks at the
        // end of the buffer.
        //******************************************************************************
        public unsafe struct tBigInt
        {
            //******************************************************************************
            // Maximum number of 32 bit blocks needed in high precision arithmetic
            // to print out 64 bit IEEE floating point values.
            //******************************************************************************
            const int c_BigInt_MaxBlocks = 35;

            //// Copy integer
            //tBigInt & operator=(tBigInt &rhs)
            //{
            //    uint length = rhs.m_length;
            //    uint* pLhsCur = m_blocks;
            //    for (uint* pRhsCur = rhs.m_blocks, *pRhsEnd = pRhsCur + length;
            //    pRhsCur != pRhsEnd;
            //    ++pLhsCur, ++pRhsCur)
            //    {
            //        *pLhsCur = *pRhsCur;
            //    }
            //    m_length = length;
            //    return *this;
            //}

            // Data accessors
            public int GetLength()        { return m_length; }
            public uint GetBlock(int idx) { return m_blocks[idx]; }

            // Zero helper functions
            public void SetZero() { m_length = 0; }
            public bool IsZero()  { return m_length == 0; }

            // Basic type accessors
            public void SetU64(ulong val)
            {
                if (val > 0xFFFFFFFF)
                {
                    m_blocks[0] = (uint)(val & 0xFFFFFFFF);
                    m_blocks[1] = (uint)(val >> 32 & 0xFFFFFFFF);
                    m_length = 2;
                }
                else if (val != 0)
                {
                    m_blocks[0] = (uint)(val & 0xFFFFFFFF);
                    m_length = 1;
                }
                else
                {
                    m_length = 0;
                }
            }

            public void SetU32(uint val)
            {
                if (val != 0)
                {
                    m_blocks[0] = val;
                    m_length = (val != 0) ? 1 : 0;
                }
                else
                {
                    m_length = 0;
                }
            }

            public uint GetU32() { return (m_length == 0) ? 0 : m_blocks[0]; }

            // Member data
            public int m_length;
            public fixed uint m_blocks[c_BigInt_MaxBlocks];
        }

        //******************************************************************************
        // Returns 0 if (lhs = rhs), negative if (lhs < rhs), positive if (lhs > rhs)
        //******************************************************************************
        private static unsafe int BigInt_Compare(in tBigInt lhs, in tBigInt  rhs)
        {
            // A bigger length implies a bigger number.
            int lengthDiff = lhs.m_length - rhs.m_length;
            if (lengthDiff != 0)
                return lengthDiff;

            // Compare blocks one by one from high to low.
            for (int i = (int)lhs.m_length - 1; i >= 0; --i)
            {
                if (lhs.m_blocks[i] == rhs.m_blocks[i])
                    continue;
                else if (lhs.m_blocks[i] > rhs.m_blocks[i])
                    return 1;
                else
                    return -1;
            }

            // no blocks differed
            return 0;
        }

        //******************************************************************************
        // result = lhs + rhs
        //******************************************************************************
        private static unsafe void BigInt_Add(out tBigInt pResult, in tBigInt lhs, in tBigInt rhs)
        {
            if (lhs.m_length < rhs.m_length)
            {
                BigInt_Add_internal(out pResult, rhs, lhs);
            }
            else
            {
                BigInt_Add_internal(out pResult, lhs, rhs);
            }
        }
        private static unsafe void BigInt_Add_internal(out tBigInt pResult, in tBigInt pLarge, in tBigInt pSmall)
        {
            int largeLen = pLarge.m_length;
            int smallLen = pSmall.m_length;

            // The output will be at least as long as the largest input
            pResult.m_length = largeLen;

            // Add each block and add carry the overflow to the next block
            ulong carry = 0;
            fixed (uint * pLargeCur1  = pLarge.m_blocks)
            fixed (uint * pSmallCur1  = pSmall.m_blocks)
            fixed (uint * pResultCur1 = pResult.m_blocks)
            {
                uint* pLargeCur = pLargeCur1;
                uint* pSmallCur = pSmallCur1;
                uint* pResultCur = pResultCur1;
                uint* pLargeEnd = pLargeCur + largeLen;
                uint* pSmallEnd = pSmallCur + smallLen;

                while (pSmallCur != pSmallEnd)
                {
                    ulong sum = carry + (ulong) (*pLargeCur) + (ulong) (*pSmallCur);
                    carry = sum >> 32;
                    (*pResultCur) = (uint)(sum & 0xFFFFFFFF);
                    ++pLargeCur;
                    ++pSmallCur;
                    ++pResultCur;
                }

                // Add the carry to any blocks that only exist in the large operand
                while (pLargeCur != pLargeEnd)
                {
                    ulong sum = carry + (ulong) (*pLargeCur);
                    carry = sum >> 32;
                    (*pResultCur) = (uint)(sum & 0xFFFFFFFF);
                    ++pLargeCur;
                    ++pResultCur;
                }

                // If there's still a carry, append a new block
                if (carry != 0)
                {
                    //RJ_ASSERT(carry == 1);
                    //RJ_ASSERT((uint)(pResultCur - pResult.m_blocks) == largeLen && (largeLen < c_BigInt_MaxBlocks));
                    *pResultCur = 1;
                    pResult.m_length = largeLen + 1;
                }
                else
                {
                    pResult.m_length = largeLen;
                }
            }
        }

        //******************************************************************************
        // result = lhs * rhs
        //******************************************************************************
        private static unsafe void BigInt_Multiply(out tBigInt pResult, in tBigInt lhs, in tBigInt rhs)
        {
            if (lhs.m_length < rhs.m_length)
            {
                BigInt_Multiply_internal(out pResult, rhs, lhs);
            }
            else
            {
                BigInt_Multiply_internal(out pResult, lhs, rhs);
            }
        }

        private static unsafe void BigInt_Multiply_internal(out tBigInt pResult, in tBigInt pLarge, in tBigInt pSmall)
        {
            // set the maximum possible result length
            int maxResultLen = pLarge.m_length + pSmall.m_length;
            // RJ_ASSERT( maxResultLen <= c_BigInt_MaxBlocks );

            // clear the result data
            // uint * pCur = pResult.m_blocks, *pEnd = pCur + maxResultLen; pCur != pEnd; ++pCur
            for (int i = 0; i < maxResultLen; i++)
                pResult.m_blocks[i] = 0;

            // perform standard long multiplication
            fixed (uint *pLargeBeg1 = pLarge.m_blocks)
            {
                uint* pLargeBeg = pLargeBeg1;
                uint* pLargeEnd = pLargeBeg + pLarge.m_length;

                // for each small block
                fixed (uint* pResultStart1 = pResult.m_blocks)
                fixed (uint* pSmallCur1 = pSmall.m_blocks)
                {
                    uint* pSmallCur = pSmallCur1;
                    uint* pSmallEnd = pSmallCur + pSmall.m_length;
                    uint* pResultStart = pResultStart1;
                    for (;  pSmallCur != pSmallEnd; ++pSmallCur, ++pResultStart)
                    {
                        // if non-zero, multiply against all the large blocks and add into the result
                        uint multiplier = *pSmallCur;
                        if (multiplier != 0)
                        {
                            uint* pLargeCur = pLargeBeg;
                            uint* pResultCur = pResultStart;
                            ulong carry = 0;
                            do
                            {
                                ulong product = (*pResultCur) + (*pLargeCur) * (ulong) multiplier + carry;
                                carry = product >> 32;
                                *pResultCur = (uint)(product & 0xFFFFFFFF);
                                ++pLargeCur;
                                ++pResultCur;
                            } while (pLargeCur != pLargeEnd);

                            //RJ_ASSERT(pResultCur < pResult.m_blocks + maxResultLen);
                            *pResultCur = (uint) (carry & 0xFFFFFFFF);
                        }
                    }

                    // check if the terminating block has no set bits
                    if (maxResultLen > 0 && pResult.m_blocks[maxResultLen - 1] == 0)
                        pResult.m_length = maxResultLen - 1;
                    else
                        pResult.m_length = maxResultLen;
                }
            }
        }

        //******************************************************************************
        // result = lhs * rhs
        //******************************************************************************
        private static unsafe void BigInt_Multiply(out tBigInt pResult, in tBigInt lhs, uint rhs)
        {
            // perform long multiplication
            uint carry = 0;
            fixed (uint* pResultCur1 = pResult.m_blocks)
            fixed (uint* pLhsCur1 = lhs.m_blocks)
            {
                uint* pResultCur = pResultCur1;
                uint* pLhsCur = pLhsCur1;
                uint* pLhsEnd = pLhsCur + lhs.m_length;
                for (; pLhsCur != pLhsEnd; ++pLhsCur, ++pResultCur)
                {
                    ulong product = (ulong) (*pLhsCur) * rhs + carry;
                    *pResultCur = (uint) (product & 0xFFFFFFFF);
                    carry = (uint)(product >> 32);
                }

                // if there is a remaining carry, grow the array
                if (carry != 0)
                {
                    // grow the array
                    //RJ_ASSERT(lhs.m_length + 1 <= c_BigInt_MaxBlocks);
                    *pResultCur = (uint) carry;
                    pResult.m_length = lhs.m_length + 1;
                }
                else
                {
                    pResult.m_length = lhs.m_length;
                }
            }
        }

        //******************************************************************************
        // result = in * 2
        //******************************************************************************
        private static unsafe void BigInt_Multiply2(out tBigInt pResult, in tBigInt input)
        {
            // shift all the blocks by one
            uint carry = 0;

            fixed (uint* pResultCur1 = pResult.m_blocks)
            fixed (uint* pLhsCur1 = input.m_blocks)
            {
                uint* pResultCur = pResultCur1;
                uint* pLhsCur = pLhsCur1;
                uint* pLhsEnd = pLhsCur + input.m_length;
                for (; pLhsCur != pLhsEnd; ++pLhsCur, ++pResultCur)
                {
                    uint cur = *pLhsCur;
                    *pResultCur = (cur << 1) | carry;
                    carry = cur >> 31;
                }

                if (carry != 0)
                {
                    // grow the array
                    // RJ_ASSERT(input.m_length + 1 <= c_BigInt_MaxBlocks);
                    *pResultCur = carry;
                    pResult.m_length = input.m_length + 1;
                }
                else
                {
                    pResult.m_length = input.m_length;
                }
            }
        }

        //******************************************************************************
        // result = result * 2
        //******************************************************************************
        private static unsafe void BigInt_Multiply2(ref tBigInt pResult)
        {
            // shift all the blocks by one
            uint carry = 0;

            fixed (uint* pCur1 = pResult.m_blocks)
            {
                uint* pCur = pCur1;
                uint* pEnd = pCur + pResult.m_length;
                for (; pCur != pEnd; ++pCur)
                {
                    uint cur = *pCur;
                    *pCur = (cur << 1) | carry;
                    carry = cur >> 31;
                }

                if (carry != 0)
                {
                    // grow the array
                    // RJ_ASSERT(pResult.m_length + 1 <= c_BigInt_MaxBlocks);
                    *pCur = carry;
                    ++pResult.m_length;
                }
            }
        }

        //******************************************************************************
        // result = result * 10
        //******************************************************************************
        private static unsafe void BigInt_Multiply10(ref tBigInt pResult)
        {
            // multiply all the blocks
            ulong carry = 0;

            fixed (uint* pCur1 = pResult.m_blocks)
            {
                uint* pCur = pCur1;
                uint* pEnd = pCur + pResult.m_length;
                for (; pCur != pEnd; ++pCur)
                {
                    ulong product = (ulong) (*pCur) * 10 + carry;
                    (*pCur) = (uint) (product & 0xFFFFFFFF);
                    carry = product >> 32;
                }

                if (carry != 0)
                {
                    // grow the array
                    //RJ_ASSERT(pResult.m_length + 1 <= c_BigInt_MaxBlocks);
                    *pCur = (uint) carry;
                    ++pResult.m_length;
                }
            }
        }

        //******************************************************************************
        //******************************************************************************
        private static readonly uint[] g_PowerOf10_U32 = new uint[]
        {
            1,          // 10 ^ 0
            10,         // 10 ^ 1
            100,        // 10 ^ 2
            1000,       // 10 ^ 3
            10000,      // 10 ^ 4
            100000,     // 10 ^ 5
            1000000,    // 10 ^ 6
            10000000,   // 10 ^ 7
        };

        //******************************************************************************
        // Note: This has a lot of wasted space in the big integer structures of the
        //       early table entries. It wouldn't be terribly hard to make the multiply
        //       function work on integer pointers with an array length instead of
        //       the tBigInt struct which would allow us to store a minimal amount of
        //       data here.
        //******************************************************************************
        private static unsafe tBigInt g_PowerOf10_Big(int i)
        {
            tBigInt result;
            // 10 ^ 8
            if (i == 0)
            {
                // { 1, { 100000000 } },
                result.m_length = 1;
                result.m_blocks[0] = 100000000;
            }
            else if (i == 1)
            {
                // 10 ^ 16
                // { 2, { 0x6fc10000, 0x002386f2 } },
                result.m_length = 2;
                result.m_blocks[0] = 0x6fc10000;
                result.m_blocks[1] = 0x002386f2;
            }
            else if (i == 2)
            {
                // 10 ^ 32
                // { 4, { 0x00000000, 0x85acef81, 0x2d6d415b, 0x000004ee, } },
                result.m_length = 4;
                result.m_blocks[0] = 0x00000000;
                result.m_blocks[1] = 0x85acef81;
                result.m_blocks[2] = 0x2d6d415b;
                result.m_blocks[3] = 0x000004ee;
            }
            else if (i == 3)
            {
                // 10 ^ 64
                // { 7, { 0x00000000, 0x00000000, 0xbf6a1f01, 0x6e38ed64, 0xdaa797ed, 0xe93ff9f4, 0x00184f03, } },
                result.m_length = 7;
                result.m_blocks[0] = 0x00000000;
                result.m_blocks[1] = 0x00000000;
                result.m_blocks[2] = 0xbf6a1f01;
                result.m_blocks[3] = 0x6e38ed64;
                result.m_blocks[4] = 0xdaa797ed;
                result.m_blocks[5] = 0xe93ff9f4;
                result.m_blocks[6] = 0x00184f03;
            }
            else if (i == 4)
            {
                // 10 ^ 128
                //{
                //    14, {
                //        0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x2e953e01, 0x03df9909, 0x0f1538fd,
                //        0x2374e42f, 0xd3cff5ec, 0xc404dc08, 0xbccdb0da, 0xa6337f19, 0xe91f2603, 0x0000024e, }
                //},
                result.m_length = 14;
                result.m_blocks[0] = 0x00000000;
                result.m_blocks[1] = 0x00000000;
                result.m_blocks[2] = 0x00000000;
                result.m_blocks[3] = 0x00000000;
                result.m_blocks[4] = 0x2e953e01;
                result.m_blocks[5] = 0x03df9909;
                result.m_blocks[6] = 0x0f1538fd;
                result.m_blocks[7] = 0x2374e42f;
                result.m_blocks[8] = 0xd3cff5ec;
                result.m_blocks[9] = 0xc404dc08;
                result.m_blocks[10] = 0xbccdb0da;
                result.m_blocks[11] = 0xa6337f19;
                result.m_blocks[12] = 0xe91f2603;
                result.m_blocks[13] = 0x0000024e;

            }
            else
            {
                // 10 ^ 256
                //{
                //    27, {
                //        0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000,
                //        0x00000000, 0x982e7c01, 0xbed3875b, 0xd8d99f72, 0x12152f87, 0x6bde50c6, 0xcf4a6e70,
                //        0xd595d80f, 0x26b2716e, 0xadc666b0, 0x1d153624, 0x3c42d35a, 0x63ff540e, 0xcc5573c0,
                //        0x65f9ef17, 0x55bc28f2, 0x80dcc7f7, 0xf46eeddc, 0x5fdcefce, 0x000553f7,
                //    }
                //}
                result.m_length = 27;
                result.m_blocks[0] = 0x00000000;
                result.m_blocks[1] = 0x00000000;
                result.m_blocks[2] = 0x00000000;
                result.m_blocks[3] = 0x00000000;
                result.m_blocks[4] = 0x00000000;
                result.m_blocks[5] = 0x00000000;
                result.m_blocks[6] = 0x00000000;
                result.m_blocks[7] = 0x00000000;
                result.m_blocks[8] = 0x982e7c01;
                result.m_blocks[9] = 0xbed3875b;
                result.m_blocks[10] = 0xd8d99f72;
                result.m_blocks[11] = 0x12152f87;
                result.m_blocks[12] = 0x6bde50c6;
                result.m_blocks[13] = 0xcf4a6e70;
                result.m_blocks[14] = 0xd595d80f;
                result.m_blocks[15] = 0x26b2716e;
                result.m_blocks[16] = 0xadc666b0;
                result.m_blocks[17] = 0x1d153624;
                result.m_blocks[18] = 0x3c42d35a;
                result.m_blocks[19] = 0x63ff540e;
                result.m_blocks[20] = 0xcc5573c0;
                result.m_blocks[21] = 0x65f9ef17;
                result.m_blocks[22] = 0x55bc28f2;
                result.m_blocks[23] = 0x80dcc7f7;
                result.m_blocks[24] = 0xf46eeddc;
                result.m_blocks[25] = 0x5fdcefce;
                result.m_blocks[26] = 0x000553f7;
            }

            return result;
        }

        //******************************************************************************
        // result = 10^exponent
        //******************************************************************************
        private static void BigInt_Pow10(out tBigInt pResult, uint exponent)
        {
            // make sure the exponent is within the bounds of the lookup table data
            // RJ_ASSERT(exponent < 512);

            // create two temporary values to reduce large integer copy operations
            tBigInt temp1 = default;
            tBigInt temp2 = default;
            ref tBigInt pCurTemp = ref temp1;
            ref tBigInt pNextTemp = ref temp2;

            // initialize the result by looking up a 32-bit power of 10 corresponding to the first 3 bits
            uint smallExponent = exponent & 0x7;
            pCurTemp.SetU32(g_PowerOf10_U32[smallExponent]);

            // remove the low bits that we used for the 32-bit lookup table
            exponent >>= 3;
            int tableIdx = 0;
            // while there are remaining bits in the exponent to be processed
            while (exponent != 0)
            {
                // if the current bit is set, multiply it with the corresponding power of 10
                if ((exponent & 1) != 0)
                {
                    // multiply into the next temporary
                    BigInt_Multiply(out pNextTemp, pCurTemp, g_PowerOf10_Big(tableIdx));

                    // swap to the next temporary
                    ref tBigInt pSwap = ref pCurTemp;
                    pCurTemp = pNextTemp;
                    pNextTemp = pSwap;
                }

                // advance to the next bit
                ++tableIdx;
                exponent >>= 1;
            }

            // output the result
            pResult = pCurTemp;
        }


        //******************************************************************************
        // result = in * 10^exponent
        //******************************************************************************
        private static unsafe void BigInt_MultiplyPow10(out tBigInt pResult, in tBigInt input, uint exponent)
        {
            // make sure the exponent is within the bounds of the lookup table data
            // RJ_ASSERT(exponent < 512);

            // create two temporary values to reduce large integer copy operations
            tBigInt temp1 = default;
            tBigInt temp2 = default;
            ref tBigInt pCurTemp = ref temp1;
            ref tBigInt pNextTemp = ref temp2;

            // initialize the result by looking up a 32-bit power of 10 corresponding to the first 3 bits
            uint smallExponent = exponent & 0x7;
            if (smallExponent != 0)
            {
                BigInt_Multiply(out pCurTemp, input, g_PowerOf10_U32[smallExponent]);
            }
            else
            {
                pCurTemp = input;
            }

            // remove the low bits that we used for the 32-bit lookup table
            exponent >>= 3;
            int tableIdx = 0;

            // while there are remaining bits in the exponent to be processed
            while (exponent != 0)
            {
                // if the current bit is set, multiply it with the corresponding power of 10
                if((exponent & 1) != 0)
                {
                    // multiply into the next temporary
                    BigInt_Multiply( out pNextTemp, pCurTemp, g_PowerOf10_Big(tableIdx) );

                    // swap to the next temporary
                    ref tBigInt pSwap = ref pCurTemp;
                    pCurTemp = pNextTemp;
                    pNextTemp = pSwap;
                }

                // advance to the next bit
                ++tableIdx;
                exponent >>= 1;
            }

            // output the result
            pResult = pCurTemp;
        }

        //******************************************************************************
        // result = 2^exponent
        //******************************************************************************
        private static unsafe void BigInt_Pow2(out tBigInt pResult, uint exponent)
        {
            int blockIdx = (int)exponent / 32;
            //RJ_ASSERT(blockIdx < c_BigInt_MaxBlocks);

            for (uint i = 0; i <= blockIdx; ++i)
                pResult.m_blocks[i] = 0;

            pResult.m_length = blockIdx + 1;

            int bitIdx = ((int)exponent % 32);
            pResult.m_blocks[blockIdx] |= (uint)(1 << bitIdx);
        }

        //******************************************************************************
        // This function will divide two large numbers under the assumption that the
        // result is within the range [0,10) and the input numbers have been shifted
        // to satisfy:
        // - The highest block of the divisor is greater than or equal to 8 such that
        //   there is enough precision to make an accurate first guess at the quotient.
        // - The highest block of the divisor is less than the maximum value on an
        //   unsigned 32-bit integer such that we can safely increment without overflow.
        // - The dividend does not contain more blocks than the divisor such that we
        //   can estimate the quotient by dividing the equivalently placed high blocks.
        //
        // quotient  = floor(dividend / divisor)
        // remainder = dividend - quotient*divisor
        //
        // pDividend is updated to be the remainder and the quotient is returned.
        //******************************************************************************
        private static unsafe uint BigInt_DivideWithRemainder_MaxQuotient9(ref tBigInt pDividend, in tBigInt divisor)
        {
            // Check that the divisor has been correctly shifted into range and that it is not
            // smaller than the dividend in length.
            //RJ_ASSERT(  !divisor.IsZero() &&
            //            divisor.m_blocks[divisor.m_length-1] >= 8 &&
            //            divisor.m_blocks[divisor.m_length-1] < 0xFFFFFFFF &&
            //            pDividend->m_length <= divisor.m_length );

            // If the dividend is smaller than the divisor, the quotient is zero and the divisor is already
            // the remainder.
            int length = divisor.m_length;
            if (pDividend.m_length < divisor.m_length)
                return 0;

            fixed (uint* pDivisorCur1 = divisor.m_blocks)
            fixed (uint* pDividendCur1 = pDividend.m_blocks)
            {
                uint* pDivisorCur = pDivisorCur1;
                uint* pDividendCur = pDividendCur1;

                uint* pFinalDivisorBlock = pDivisorCur + length - 1;
                uint* pFinalDividendBlock = pDividendCur + length - 1;

                // Compute an estimated quotient based on the high block value. This will either match the actual quotient or
                // undershoot by one.
                uint quotient = *pFinalDividendBlock / (*pFinalDivisorBlock + 1);
                //RJ_ASSERT(quotient <= 9);

                // Divide out the estimated quotient
                if (quotient != 0)
                {
                    // dividend = dividend - divisor*quotient
                    ulong borrow = 0;
                    ulong carry = 0;
                    do
                    {
                        ulong product = (ulong) *pDivisorCur * (ulong) quotient + carry;
                        carry = product >> 32;

                        ulong difference = (ulong) *pDividendCur - (product & 0xFFFFFFFF) - borrow;
                        borrow = (difference >> 32) & 1;

                        *pDividendCur = (uint) (difference & 0xFFFFFFFF);

                        ++pDivisorCur;
                        ++pDividendCur;
                    } while (pDivisorCur <= pFinalDivisorBlock);

                    // remove all leading zero blocks from dividend
                    while (length > 0 && pDividend.m_blocks[length - 1] == 0)
                        --length;

                    pDividend.m_length = length;
                }

                // If the dividend is still larger than the divisor, we overshot our estimate quotient. To correct,
                // we increment the quotient and subtract one more divisor from the dividend.
                if (BigInt_Compare(pDividend, divisor) >= 0)
                {
                    ++quotient;

                    // dividend = dividend - divisor
                    pDivisorCur = pDivisorCur1;
                    pDividendCur = pDividendCur1;

                    ulong borrow = 0;
                    do
                    {
                        ulong difference = (ulong) *pDividendCur - (ulong) *pDivisorCur - borrow;
                        borrow = (difference >> 32) & 1;

                        *pDividendCur = (uint)(difference & 0xFFFFFFFF);

                        ++pDivisorCur;
                        ++pDividendCur;
                    } while (pDivisorCur <= pFinalDivisorBlock);

                    // remove all leading zero blocks from dividend
                    while (length > 0 && pDividend.m_blocks[length - 1] == 0)
                        --length;

                    pDividend.m_length = length;
                }

                return quotient;
            }
        }


        //******************************************************************************
        // result = result << shift
        //******************************************************************************
        private static unsafe void BigInt_ShiftLeft(ref tBigInt pResult, uint shift)
        {
            // RJ_ASSERT( shift != 0 );

            int shiftBlocks = (int)shift / 32;
            int shiftBits = (int)shift % 32;

            int inLength    = pResult.m_length;
            // RJ_ASSERT( inLength + shiftBlocks <= c_BigInt_MaxBlocks );

            // check if the shift is block aligned
            if (shiftBits == 0)
            {
                // process blocks high to low so that we can safely process in place
                fixed (uint* pInBlocks1 = pResult.m_blocks)
                {
                    uint* pInBlocks = pInBlocks1;
                    uint* pInCur = pInBlocks + inLength - 1;
                    uint* pOutCur = pInCur + shiftBlocks;

                    // copy blocks from high to low
                    for (; pInCur >= pInBlocks; --pInCur, --pOutCur)
                    {
                        *pOutCur = *pInCur;
                    }
                }

                // zero the remaining low blocks
                for ( uint i = 0; i < shiftBlocks; ++i)
                    pResult.m_blocks[i] = 0;

                pResult.m_length += shiftBlocks;
            }
            // else we need to shift partial blocks
            else
            {
                int inBlockIdx  = inLength - 1;
                int outBlockIdx = inLength + shiftBlocks;

                // set the length to hold the shifted blocks
                //RJ_ASSERT( outBlockIdx < c_BigInt_MaxBlocks );
                pResult.m_length = outBlockIdx + 1;

                // output the initial blocks
                int lowBitsShift = (32 - shiftBits);
                uint highBits = 0;
                uint block = pResult.m_blocks[inBlockIdx];
                uint lowBits = block >> lowBitsShift;
                while ( inBlockIdx > 0 )
                {
                    pResult.m_blocks[outBlockIdx] = highBits | lowBits;
                    highBits = block << shiftBits;

                    --inBlockIdx;
                    --outBlockIdx;

                    block = pResult.m_blocks[inBlockIdx];
                    lowBits = block >> lowBitsShift;
                }

                // output the final blocks
                // RJ_ASSERT( outBlockIdx == shiftBlocks + 1 );
                pResult.m_blocks[outBlockIdx] = highBits | lowBits;
                pResult.m_blocks[outBlockIdx-1] = block << shiftBits;

                // zero the remaining low blocks
                for ( uint i = 0; i < shiftBlocks; ++i)
                    pResult.m_blocks[i] = 0;

                // check if the terminating block has no set bits
                if (pResult.m_blocks[pResult.m_length - 1] == 0)
                    --pResult.m_length;
            }
        }

        //******************************************************************************
        // Different modes for terminating digit output
        //******************************************************************************
        public enum CutoffMode
        {
            Unique,          // as many digits as necessary to print a uniquely identifiable number
            TotalLength,     // up to cutoffNumber significant digits
            FractionLength,  // up to cutoffNumber significant digits past the decimal point
        };

        //******************************************************************************
        // This is an implementation the Dragon4 algorithm to convert a binary number
        // in floating point format to a decimal number in string format. The function
        // returns the number of digits written to the output buffer and the output is
        // not NUL terminated.
        //
        // The floating point input value is (mantissa * 2^exponent).
        //
        // See the following papers for more information on the algorithm:
        //  "How to Print Floating-Point Numbers Accurately"
        //    Steele and White
        //    http://kurtstephens.com/files/p372-steele.pdf
        //  "Printing Floating-Point Numbers Quickly and Accurately"
        //    Burger and Dybvig
        //    http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.72.4656&rep=rep1&type=pdf
        //******************************************************************************
        private static unsafe uint Dragon4
        (
            ulong          mantissa,           // value significand
            int          exponent,           // value exponent in base 2
            uint          mantissaHighBitIdx, // index of the highest set mantissa bit
            bool            hasUnequalMargins,  // is the high margin twice as large as the low margin
            CutoffMode cutoffMode,         // how to determine output length
            int                 cutoffNumber,       // parameter to the selected cutoffMode
            byte*               pOutBuffer,         // buffer to output into
            uint                bufferSize,         // maximum characters that can be printed to pOutBuffer
            out int             pOutExponent        // the base 10 exponent of the first digit
        )
        {
            byte* pCurDigit = pOutBuffer;

            // RJ_ASSERT( bufferSize > 0 );

            // if the mantissa is zero, the value is zero regardless of the exponent
            if (mantissa == 0)
            {
                *pCurDigit = (byte)'0';
                pOutExponent = 0;
                return 1;
            }

            // compute the initial state in integral form such that
            //  value     = scaledValue / scale
            //  marginLow = scaledMarginLow / scale
            tBigInt scale = default;              // positive scale applied to value and margin such that they can be
                                        //  represented as whole numbers
            tBigInt scaledValue = default;        // scale * mantissa
            tBigInt scaledMarginLow = default;    // scale * 0.5 * (distance between this floating-point number and its
                                        //  immediate lower value)

            // For normalized IEEE floating point values, each time the exponent is incremented the margin also
            // doubles. That creates a subset of transition numbers where the high margin is twice the size of
            // the low margin.
            tBigInt * pScaledMarginHigh;
            tBigInt optionalMarginHigh = default;

            if ( hasUnequalMargins )
            {
                // if we have no fractional component
                if (exponent > 0)
                {
                    // 1) Expand the input value by multiplying out the mantissa and exponent. This represents
                    //    the input value in its whole number representation.
                    // 2) Apply an additional scale of 2 such that later comparisons against the margin values
                    //    are simplified.
                    // 3) Set the margin value to the lowest mantissa bit's scale.

                    // scaledValue      = 2 * 2 * mantissa*2^exponent
                    scaledValue.SetU64( 4 * mantissa );
                    BigInt_ShiftLeft(ref scaledValue, (uint)exponent);

                    // scale            = 2 * 2 * 1
                    scale.SetU32( 4 );

                    // scaledMarginLow  = 2 * 2^(exponent-1)
                    BigInt_Pow2( out scaledMarginLow, (uint)exponent );

                    // scaledMarginHigh = 2 * 2 * 2^(exponent-1)
                    BigInt_Pow2( out optionalMarginHigh, (uint)(exponent + 1));
                }
                // else we have a fractional exponent
                else
                {
                    // In order to track the mantissa data as an integer, we store it as is with a large scale

                    // scaledValue      = 2 * 2 * mantissa
                    scaledValue.SetU64( 4 * mantissa );

                    // scale            = 2 * 2 * 2^(-exponent)
                    BigInt_Pow2(out scale, (uint)(-exponent + 2));

                    // scaledMarginLow  = 2 * 2^(-1)
                    scaledMarginLow.SetU32( 1 );

                    // scaledMarginHigh = 2 * 2 * 2^(-1)
                    optionalMarginHigh.SetU32( 2 );
                }

                // the high and low margins are different
                pScaledMarginHigh = &optionalMarginHigh;
            }
            else
            {
                // if we have no fractional component
                if (exponent > 0)
                {
                    // 1) Expand the input value by multiplying out the mantissa and exponent. This represents
                    //    the input value in its whole number representation.
                    // 2) Apply an additional scale of 2 such that later comparisons against the margin values
                    //    are simplified.
                    // 3) Set the margin value to the lowest mantissa bit's scale.

                    // scaledValue     = 2 * mantissa*2^exponent
                    scaledValue.SetU64( 2 * mantissa );
                    BigInt_ShiftLeft(ref scaledValue, (uint)exponent);

                    // scale           = 2 * 1
                    scale.SetU32( 2 );

                    // scaledMarginLow = 2 * 2^(exponent-1)
                    BigInt_Pow2(out scaledMarginLow, (uint)exponent );
                }
                // else we have a fractional exponent
                else
                {
                    // In order to track the mantissa data as an integer, we store it as is with a large scale

                    // scaledValue     = 2 * mantissa
                    scaledValue.SetU64( 2 * mantissa );

                    // scale           = 2 * 2^(-exponent)
                    BigInt_Pow2(out scale, (uint)(-exponent + 1));

                    // scaledMarginLow = 2 * 2^(-1)
                    scaledMarginLow.SetU32( 1 );
                }

                // the high and low margins are equal
                pScaledMarginHigh = &scaledMarginLow;
            }

            // Compute an estimate for digitExponent that will be correct or undershoot by one.
            // This optimization is based on the paper "Printing Floating-Point Numbers Quickly and Accurately"
            // by Burger and Dybvig http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.72.4656&rep=rep1&type=pdf
            // We perform an additional subtraction of 0.69 to increase the frequency of a failed estimate
            // because that lets us take a faster branch in the code. 0.69 is chosen because 0.69 + log10(2) is
            // less than one by a reasonable epsilon that will account for any floating point error.
            //
            // We want to set digitExponent to floor(log10(v)) + 1
            //  v = mantissa*2^exponent
            //  log2(v) = log2(mantissa) + exponent;
            //  log10(v) = log2(v) * log10(2)
            //  floor(log2(v)) = mantissaHighBitIdx + exponent;
            //  log10(v) - log10(2) < (mantissaHighBitIdx + exponent) * log10(2) <= log10(v)
            //  log10(v) < (mantissaHighBitIdx + exponent) * log10(2) + log10(2) <= log10(v) + log10(2)
            //  floor( log10(v) ) < ceil( (mantissaHighBitIdx + exponent) * log10(2) ) <= floor( log10(v) ) + 1
            const double log10_2 = 0.30102999566398119521373889472449;
            var digitExponentDoubleValue = (double) ((int) mantissaHighBitIdx + exponent) * log10_2 - 0.69;
            digitExponentDoubleValue = Math.Ceiling(digitExponentDoubleValue);
            int digitExponent = (int)digitExponentDoubleValue;

            // if the digit exponent is smaller than the smallest desired digit for fractional cutoff,
            // pull the digit back into legal range at which point we will round to the appropriate value.
            // Note that while our value for digitExponent is still an estimate, this is safe because it
            // only increases the number. This will either correct digitExponent to an accurate value or it
            // will clamp it above the accurate value.
            if (cutoffMode == CutoffMode.FractionLength && digitExponent <= -(int)cutoffNumber)
            {
                digitExponent = -(int)cutoffNumber + 1;
            }

            // Divide value by 10^digitExponent.
            if (digitExponent > 0)
            {
                // The exponent is positive creating a division so we multiply up the scale.
                tBigInt temp;
                BigInt_MultiplyPow10( out temp, scale, (uint)digitExponent );
                scale = temp;
            }
            else if (digitExponent < 0)
            {
                // The exponent is negative creating a multiplication so we multiply up the scaledValue,
                // scaledMarginLow and scaledMarginHigh.
                tBigInt pow10;
                BigInt_Pow10(out pow10, (uint)(-digitExponent));

                tBigInt temp;
                BigInt_Multiply( out temp, scaledValue, pow10);
                scaledValue = temp;

                BigInt_Multiply( out temp, scaledMarginLow, pow10);
                scaledMarginLow = temp;

                if (pScaledMarginHigh != &scaledMarginLow)
                    BigInt_Multiply2( out *pScaledMarginHigh, scaledMarginLow );
            }

            // If (value >= 1), our estimate for digitExponent was too low
            if( BigInt_Compare(scaledValue,scale) >= 0 )
            {
                // The exponent estimate was incorrect.
                // Increment the exponent and don't perform the premultiply needed
                // for the first loop iteration.
                digitExponent = digitExponent + 1;
            }
            else
            {
                // The exponent estimate was correct.
                // Multiply larger by the output base to prepare for the first loop iteration.
                BigInt_Multiply10( ref scaledValue );
                BigInt_Multiply10( ref scaledMarginLow );
                if (pScaledMarginHigh != &scaledMarginLow)
                    BigInt_Multiply2( out *pScaledMarginHigh, scaledMarginLow );
            }

            // Compute the cutoff exponent (the exponent of the final digit to print).
            // Default to the maximum size of the output buffer.
            int cutoffExponent = digitExponent - (int)bufferSize;

            if (cutoffNumber != -1)
            {
                switch (cutoffMode)
                {
                    // print digits until we pass the accuracy margin limits or buffer size
                    case CutoffMode.Unique:
                        break;

                    // print cutoffNumber of digits or until we reach the buffer size
                    case CutoffMode.TotalLength:
                        {
                            int desiredCutoffExponent = digitExponent - cutoffNumber;
                            if (desiredCutoffExponent > cutoffExponent)
                                cutoffExponent = desiredCutoffExponent;
                        }
                        break;

                    // print cutoffNumber digits past the decimal point or until we reach the buffer size
                    case CutoffMode.FractionLength:
                        {
                            int desiredCutoffExponent = -cutoffNumber;
                            if (desiredCutoffExponent > cutoffExponent)
                                cutoffExponent = desiredCutoffExponent;
                        }
                        break;
                }
            }

            // Output the exponent of the first digit we will print
            pOutExponent = digitExponent-1;

            // In preparation for calling BigInt_DivideWithRemainder_MaxQuotient9(),
            // we need to scale up our values such that the highest block of the denominator
            // is greater than or equal to 8. We also need to guarantee that the numerator
            // can never have a length greater than the denominator after each loop iteration.
            // This requires the highest block of the denominator to be less than or equal to
            // 429496729 which is the highest number that can be multiplied by 10 without
            // overflowing to a new block.
            // RJ_ASSERT( scale.GetLength() > 0 );
            uint hiBlock = scale.GetBlock( scale.GetLength() - 1 );
            if (hiBlock < 8 || hiBlock > 429496729)
            {
                // Perform a bit shift on all values to get the highest block of the denominator into
                // the range [8,429496729]. We are more likely to make accurate quotient estimations
                // in BigInt_DivideWithRemainder_MaxQuotient9() with higher denominator values so
                // we shift the denominator to place the highest bit at index 27 of the highest block.
                // This is safe because (2^28 - 1) = 268435455 which is less than 429496729. This means
                // that all values with a highest bit at index 27 are within range.
                uint hiBlockLog2 = LogBase2(hiBlock);
                // RJ_ASSERT(hiBlockLog2 < 3 || hiBlockLog2 > 27);
                uint shift = (32 + 27 - hiBlockLog2) % 32;

                BigInt_ShiftLeft( ref scale, shift );
                BigInt_ShiftLeft( ref scaledValue, shift);
                BigInt_ShiftLeft( ref scaledMarginLow, shift);
                if (pScaledMarginHigh != &scaledMarginLow)
                    BigInt_Multiply2( out *pScaledMarginHigh, scaledMarginLow );
            }

            // These values are used to inspect why the print loop terminated so we can properly
            // round the final digit.
            bool      low;            // did the value get within marginLow distance from zero
            bool      high;           // did the value get within marginHigh distance from one
            uint    outputDigit;    // current digit being output

            if (cutoffMode == CutoffMode.Unique || cutoffNumber == -1)
            {
                // For the unique cutoff mode, we will try to print until we have reached a level of
                // precision that uniquely distinguishes this value from its neighbors. If we run
                // out of space in the output buffer, we terminate early.
                for (;;)
                {
                    digitExponent = digitExponent-1;

                    // divide out the scale to extract the digit
                    outputDigit = BigInt_DivideWithRemainder_MaxQuotient9(ref scaledValue, scale);
                    //RJ_ASSERT( outputDigit < 10 );

                    // update the high end of the value
                    tBigInt scaledValueHigh;
                    BigInt_Add( out scaledValueHigh, scaledValue, *pScaledMarginHigh );

                    // stop looping if we are far enough away from our neighboring values
                    // or if we have reached the cutoff digit
                    low = BigInt_Compare(scaledValue, scaledMarginLow) < 0;
                    high = BigInt_Compare(scaledValueHigh, scale) > 0;
                    if (low | high | (digitExponent == cutoffExponent))
                        break;

                    // store the output digit
                    *pCurDigit = (byte)('0' + outputDigit);
                    ++pCurDigit;

                    // multiply larger by the output base
                    BigInt_Multiply10( ref scaledValue );
                    BigInt_Multiply10( ref scaledMarginLow );
                    if (pScaledMarginHigh != &scaledMarginLow)
                        BigInt_Multiply2( out *pScaledMarginHigh, scaledMarginLow );
                }
            }
            else
            {
                // For length based cutoff modes, we will try to print until we
                // have exhausted all precision (i.e. all remaining digits are zeros) or
                // until we reach the desired cutoff digit.
                low = false;
                high = false;

                for (;;)
                {
                    digitExponent = digitExponent-1;

                    // divide out the scale to extract the digit
                    outputDigit = BigInt_DivideWithRemainder_MaxQuotient9(ref scaledValue, scale);
                    //RJ_ASSERT( outputDigit < 10 );

                    if ( scaledValue.IsZero() | (digitExponent == cutoffExponent) )
                        break;

                    // store the output digit
                    *pCurDigit = (byte)('0' + outputDigit);
                    ++pCurDigit;

                    // multiply larger by the output base
                    BigInt_Multiply10(ref scaledValue);
                }
            }

            // round off the final digit
            // default to rounding down if value got too close to 0
            bool roundDown = low;

            // if it is legal to round up and down
            if (low == high)
            {
                // round to the closest digit by comparing value with 0.5. To do this we need to convert
                // the inequality to large integer values.
                //  compare( value, 0.5 )
                //  compare( scale * value, scale * 0.5 )
                //  compare( 2 * scale * value, scale )
                BigInt_Multiply2(ref scaledValue);
                int compare = BigInt_Compare(scaledValue, scale);
                roundDown = compare < 0;

                // if we are directly in the middle, round towards the even digit (i.e. IEEE rouding rules)
                if (compare == 0)
                    roundDown = (outputDigit & 1) == 0;
            }

            // print the rounded digit
            if (roundDown)
            {
                *pCurDigit = (byte)('0' + outputDigit);
                ++pCurDigit;
            }
            else
            {
                // handle rounding up
                if (outputDigit == 9)
                {
                    // find the first non-nine prior digit
                    for (;;)
                    {
                        // if we are at the first digit
                        if (pCurDigit == pOutBuffer)
                        {
                            // output 1 at the next highest exponent
                            *pCurDigit = (byte)'1';
                            ++pCurDigit;
                            pOutExponent += 1;
                            break;
                        }

                        --pCurDigit;
                        if (*pCurDigit != (byte)'9')
                        {
                            // increment the digit
                            *pCurDigit += 1;
                            ++pCurDigit;
                            break;
                        }
                    }
                }
                else
                {
                    // values in the range [0,8] can perform a simple round up
                    *pCurDigit = (byte)((byte)'0' + outputDigit + 1);
                    ++pCurDigit;
                }
            }

            // return the number of digits output
            uint outputLen = (uint)(pCurDigit - pOutBuffer);
            // RJ_ASSERT(outputLen <= bufferSize);
            return outputLen;
        }

        //******************************************************************************
        //******************************************************************************
        public enum PrintFloatFormat
        {
            Positional,    // [-]ddddd.dddd
            Scientific,    // [-]d.dddde[sign]ddd
        }

        //******************************************************************************\
        // Helper union to decompose a 32-bit IEEE float.
        // sign:      1 bit
        // exponent:  8 bits
        // mantissa: 23 bits
        //******************************************************************************
        [StructLayout(LayoutKind.Explicit)]
        public struct tFloatUnion32
        {
            public bool IsNegative() { return (m_integer >> 31) != 0; }
            public uint GetExponent() { return (m_integer >> 23) & 0xFF; }
            public uint GetMantissa() { return m_integer & 0x7FFFFF; }

            [FieldOffset(0)]
            public float m_floatingPoint;

            [FieldOffset(0)]
            public uint m_integer;
        };

        //******************************************************************************
        // Helper union to decompose a 64-bit IEEE float.
        // sign:      1 bit
        // exponent: 11 bits
        // mantissa: 52 bits
        //******************************************************************************
        [StructLayout(LayoutKind.Explicit)]
        public struct tFloatUnion64
        {
            public bool   IsNegative() { return (m_integer >> 63) != 0; }
            public uint GetExponent() { return (uint)((m_integer >> 52) & 0x7FF); }
            public ulong GetMantissa() { return m_integer & 0xFFFFFFFFFFFFFUL; }

            [FieldOffset(0)]
            public double m_floatingPoint;

            [FieldOffset(0)]
            public ulong m_integer;
        };


        //******************************************************************************
        // Outputs the positive number with positional notation: ddddd.dddd
        // The output is always NUL terminated and the output length (not including the
        // NUL) is returned.
        //******************************************************************************
        private static unsafe int FormatPositional
        (
            byte* pOutBuffer,         // buffer to output into
            uint bufferSize,         // maximum characters that can be printed to pOutBuffer
            ulong mantissa,           // value significand
            int exponent,           // value exponent in base 2
            uint mantissaHighBitIdx, // index of the highest set mantissa bit
            bool hasUnequalMargins,  // is the high margin twice as large as the low margin
            int precision           // Negative prints as many digits as are needed for a unique
                                     //  number. Positive specifies the maximum number of
                                     //  significant digits to print past the decimal point.
        )
        {
            //RJ_ASSERT(bufferSize > 0);

            int printExponent;
            uint numPrintDigits;

            uint maxPrintLen = bufferSize - 1;

            if (precision < 0)
            {
                numPrintDigits = Dragon4(mantissa,
                                            exponent,
                                            mantissaHighBitIdx,
                                            hasUnequalMargins,
                                            CutoffMode.Unique,
                                            0,
                                            pOutBuffer,
                                            maxPrintLen,
                                            out printExponent);
            }
            else
            {
                numPrintDigits = Dragon4(mantissa,
                                            exponent,
                                            mantissaHighBitIdx,
                                            hasUnequalMargins,
                                            CutoffMode.FractionLength,
                                            precision,
                                            pOutBuffer,
                                            maxPrintLen,
                                            out printExponent);
            }

            //RJ_ASSERT(numPrintDigits > 0);
            //RJ_ASSERT(numPrintDigits <= bufferSize);

            // track the number of digits past the decimal point that have been printed
            uint numFractionDigits = 0;

            // if output has a whole number
            if (printExponent >= 0)
            {
                // leave the whole number at the start of the buffer
                uint numWholeDigits = (uint)(printExponent + 1);
                if (numPrintDigits < numWholeDigits)
                {
                    // don't overflow the buffer
                    if (numWholeDigits > maxPrintLen)
                        numWholeDigits = maxPrintLen;

                    // add trailing zeros up to the decimal point
                    for (; numPrintDigits < numWholeDigits; ++numPrintDigits)
                        pOutBuffer[numPrintDigits] = (byte)'0';
                }
                // insert the decimal point prior to the fraction
                else if (numPrintDigits > (uint)numWholeDigits)
                {
                    numFractionDigits = numPrintDigits - numWholeDigits;
                    uint maxFractionDigits = maxPrintLen - numWholeDigits - 1;
                    if (numFractionDigits > maxFractionDigits)
                        numFractionDigits = maxFractionDigits;
                    BurstUnsafeUtility.MemCpy(pOutBuffer + numWholeDigits + 1, pOutBuffer + numWholeDigits, numFractionDigits);
                    pOutBuffer[numWholeDigits] = (byte)'.';
                    numPrintDigits = numWholeDigits + 1 + numFractionDigits;
                }
            }
            else
            {
                // shift out the fraction to make room for the leading zeros
                if (maxPrintLen > 2)
                {
                    uint numFractionZeros = (uint)( - printExponent - 1);
                    uint maxFractionZeros = maxPrintLen - 2;
                    if (numFractionZeros > maxFractionZeros)
                        numFractionZeros = maxFractionZeros;

                    uint digitsStartIdx = 2 + numFractionZeros;

                    // shift the significant digits right such that there is room for leading zeros
                    numFractionDigits = numPrintDigits;
                    uint maxFractionDigits = maxPrintLen - digitsStartIdx;
                    if (numFractionDigits > maxFractionDigits)
                        numFractionDigits = maxFractionDigits;
                    BurstUnsafeUtility.MemCpy(pOutBuffer + digitsStartIdx, pOutBuffer, numFractionDigits);
                    // insert the leading zeros
                    for (uint i = 2; i < digitsStartIdx; ++i)
                        pOutBuffer[i] = (byte)'0';

                    // update the counts
                    numFractionDigits += numFractionZeros;
                    numPrintDigits = numFractionDigits;
                }

                // add the decimal point
                if (maxPrintLen > 1)
                {
                    pOutBuffer[1] = (byte)'.';
                    numPrintDigits += 1;
                }

                // add the initial zero
                if (maxPrintLen > 0)
                {
                    pOutBuffer[0] = (byte)'0';
                    numPrintDigits += 1;
                }
            }

            // add trailing zeros up to precision length
            if (precision > (int)numFractionDigits && numPrintDigits < maxPrintLen)
            {
                // add a decimal point if this is the first fractional digit we are printing
                if (numFractionDigits == 0)
                {
                    pOutBuffer[numPrintDigits++] = (byte)'.';
                }

                // compute the number of trailing zeros needed
                uint totalDigits = (uint)(numPrintDigits + (precision - (int)numFractionDigits));
                if (totalDigits > maxPrintLen)
                    totalDigits = maxPrintLen;

                for (; numPrintDigits < totalDigits; ++numPrintDigits)
                    pOutBuffer[numPrintDigits] = (byte)'0';
            }

            // terminate the buffer
            //RJ_ASSERT(numPrintDigits <= maxPrintLen);
            //pOutBuffer[numPrintDigits] = '\0';

            return (int)numPrintDigits;
        }



        //******************************************************************************
        // Outputs the positive number with scientific notation: d.dddde[sign]ddd
        // The output is always NUL terminated and the output length (not including the
        // NUL) is returned.
        //******************************************************************************
        private static unsafe int FormatScientific
        (
            byte* pOutBuffer,         // buffer to output into
            uint bufferSize,         // maximum characters that can be printed to pOutBuffer
            ulong mantissa,           // value significand
            int exponent,           // value exponent in base 2
            uint mantissaHighBitIdx, // index of the highest set mantissa bit
            bool hasUnequalMargins,  // is the high margin twice as large as the low margin
            int precision           // Negative prints as many digits as are needed for a unique
                                     //  number. Positive specifies the maximum number of
                                     //  significant digits to print past the decimal point.
        )
        {
            //RJ_ASSERT(bufferSize > 0);

            int printExponent;
            uint numPrintDigits;

            if (precision < 0)
            {
                numPrintDigits = Dragon4(mantissa,
                                            exponent,
                                            mantissaHighBitIdx,
                                            hasUnequalMargins,
                                            CutoffMode.Unique,
                                            0,
                                            pOutBuffer,
                                            bufferSize,
                                            out printExponent);
            }
            else
            {
                numPrintDigits = Dragon4(mantissa,
                                            exponent,
                                            mantissaHighBitIdx,
                                            hasUnequalMargins,
                                            CutoffMode.TotalLength,
                                            (precision + 1),
                                            pOutBuffer,
                                            bufferSize,
                                            out printExponent);
            }

            //RJ_ASSERT(numPrintDigits > 0);
            //RJ_ASSERT(numPrintDigits <= bufferSize);

            byte* pCurOut = pOutBuffer;

            // keep the whole number as the first digit
            if (bufferSize > 1)
            {
                pCurOut += 1;
                bufferSize -= 1;
            }

            // insert the decimal point prior to the fractional number
            uint numFractionDigits = numPrintDigits - 1;
            if (numFractionDigits > 0 && bufferSize > 1)
            {
                uint maxFractionDigits = bufferSize - 2;
                if (numFractionDigits > maxFractionDigits)
                    numFractionDigits = maxFractionDigits;
                BurstUnsafeUtility.MemCpy(pCurOut + 1, pCurOut, numFractionDigits);
                pCurOut[0] = (byte)'.';
                pCurOut += (1 + numFractionDigits);
                bufferSize -= (1 + numFractionDigits);
            }

            // add trailing zeros up to precision length
            if (precision > (int)numFractionDigits && bufferSize > 1)
            {
                // add a decimal point if this is the first fractional digit we are printing
                if (numFractionDigits == 0)
                {
                    *pCurOut = (byte)'.';
                    ++pCurOut;
                    --bufferSize;
                }

                // compute the number of trailing zeros needed
                uint numZeros = (uint)(precision - numFractionDigits);
                if (numZeros > bufferSize - 1)
                    numZeros = bufferSize - 1;

                for (byte* pEnd = pCurOut + numZeros; pCurOut < pEnd; ++pCurOut)
                    *pCurOut = (byte)'0';
            }

            // print the exponent into a local buffer and copy into output buffer
            if (bufferSize > 1)
            {
                var exponentBuffer = stackalloc byte[5];
                exponentBuffer[0] = (byte)'e';
                if (printExponent >= 0)
                {
                    exponentBuffer[1] = (byte)'+';
                }
                else
                {
                    exponentBuffer[1] = (byte)'-';
                    printExponent = -printExponent;
                }

                //RJ_ASSERT(printExponent < 1000);
                uint hundredsPlace = (uint)(printExponent / 100);
                uint tensPlace = (uint)((printExponent - hundredsPlace * 100) / 10);
                uint onesPlace = (uint)((printExponent - hundredsPlace * 100 - tensPlace * 10));

                exponentBuffer[2] = (byte)('0' + hundredsPlace);
                exponentBuffer[3] = (byte)('0' + tensPlace);
                exponentBuffer[4] = (byte)('0' + onesPlace);

                // copy the exponent buffer into the output
                uint maxExponentSize = bufferSize - 1;
                uint exponentSize = (5 < maxExponentSize) ? 5 : maxExponentSize;

                BurstUnsafeUtility.MemCpy(pCurOut, exponentBuffer, exponentSize);
                pCurOut += exponentSize;
                bufferSize -= exponentSize;
            }

            //RJ_ASSERT(bufferSize > 0);
            //pCurOut[0] = '\0';

            return (int)(pCurOut - pOutBuffer);
        }

        //******************************************************************************
        // Print special case values for infinities and NaNs.
        // The output string is always NUL terminated and the string length (not
        // including the NUL) is returned.
        //******************************************************************************
        private static readonly byte[] InfinityString = new byte[]
        {
            (byte) 'I',
            (byte) 'n',
            (byte) 'f',
            (byte) 'i',
            (byte) 'n',
            (byte) 'i',
            (byte) 't',
            (byte) 'y',
        };

        private static readonly byte[] NanString = new byte[]
        {
            (byte) 'N',
            (byte) 'a',
            (byte) 'N',
        };

        private static unsafe void FormatInfinityNaN(byte* dest, ref int destIndex, int destLength, ulong mantissa, bool isNegative, FormatOptions formatOptions)
        {
            //RJ_ASSERT(bufferSize > 0);
            int length = mantissa == 0 ? 8 + (isNegative ? 1 : 0) : 3;
            int align = formatOptions.AlignAndSize;

            // left align
            if (AlignLeft(dest, ref destIndex, destLength, align, length)) return;

            // Check for infinity
            if (mantissa == 0)
            {
                if (isNegative)
                {
                    if (destIndex >= destLength) return;
                    dest[destIndex++] = (byte)'-';
                }

                for (int i = 0; i < 8; i++)
                {
                    if (destIndex >= destLength) return;
                    dest[destIndex++] = InfinityString[i];
                }
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    if (destIndex >= destLength) return;
                    dest[destIndex++] = NanString[i];
                }
            }

            // right align
            AlignRight(dest, ref destIndex, destLength, align, length);
        }

        // ------------------------------------------------------------------------------
        // Part of the following code is taking some constants and code from
        // https://github.com/dotnet/runtime/blob/75036ffec9473dd1d948c052c041fdedd7784ac9/src/libraries/System.Private.CoreLib/src/System/Number.Formatting.cs
        // Licensed to the .NET Foundation under one or more agreements.
        // The .NET Foundation licenses this file to you under the MIT license.
        // See the https://github.com/dotnet/runtime/blob/master/LICENSE.TXT file for more information.
        // ------------------------------------------------------------------------------

        // SinglePrecision and DoublePrecision represent the maximum number of digits required
        // to guarantee that any given Single or Double can roundtrip. Some numbers may require
        // less, but none will require more.
        private const int SinglePrecision = 9;
        private const int DoublePrecision = 17;

        internal const int SingleNumberBufferLength = SinglePrecision + 1; // + zero
        internal const int DoubleNumberBufferLength = DoublePrecision + 1; // + zero

        // SinglePrecisionCustomFormat and DoublePrecisionCustomFormat are used to ensure that
        // custom format strings return the same string as in previous releases when the format
        // would return x digits or less (where x is the value of the corresponding constant).
        // In order to support more digits, we would need to update ParseFormatSpecifier to pre-parse
        // the format and determine exactly how many digits are being requested and whether they
        // represent "significant digits" or "digits after the decimal point".
        private const int SinglePrecisionCustomFormat = 7;
        private const int DoublePrecisionCustomFormat = 15;

        /// <summary>
        /// Format a float 32-bit to a general format to the specified destination buffer.
        /// </summary>
        /// <param name="dest">Destination buffer.</param>
        /// <param name="destIndex">Current index in destination buffer.</param>
        /// <param name="destLength">Maximum length of destination buffer.</param>
        /// <param name="value">The float 32 value to format.</param>
        /// <param name="formatOptions">Formatting options.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe void ConvertFloatToString(byte* dest, ref int destIndex, int destLength, float value, FormatOptions formatOptions)
        {
            // deconstruct the floating point value
            tFloatUnion32 floatUnion = default;
            floatUnion.m_floatingPoint = value;
            uint floatExponent = floatUnion.GetExponent();
            uint floatMantissa = floatUnion.GetMantissa();

            // if this is a special value
            if (floatExponent == 0xFF)
            {
                FormatInfinityNaN(dest, ref destIndex, destLength, floatMantissa, floatUnion.IsNegative(), formatOptions);
            }
            // else this is a number
            else
            {
                // factor the value into its parts
                uint mantissa;
                int exponent;
                uint mantissaHighBitIdx;
                bool hasUnequalMargins;
                if (floatExponent != 0)
                {
                    // normalized
                    // The floating point equation is:
                    //  value = (1 + mantissa/2^23) * 2 ^ (exponent-127)
                    // We convert the integer equation by factoring a 2^23 out of the exponent
                    //  value = (1 + mantissa/2^23) * 2^23 * 2 ^ (exponent-127-23)
                    //  value = (2^23 + mantissa) * 2 ^ (exponent-127-23)
                    // Because of the implied 1 in front of the mantissa we have 24 bits of precision.
                    //   m = (2^23 + mantissa)
                    //   e = (exponent-127-23)
                    mantissa = (uint)((1UL << 23) | floatMantissa);
                    exponent = (int)(floatExponent - 127 - 23);
                    mantissaHighBitIdx = 23;
                    hasUnequalMargins = (floatExponent != 1) && (floatMantissa == 0);
                }
                else
                {
                    // denormalized
                    // The floating point equation is:
                    //  value = (mantissa/2^23) * 2 ^ (1-127)
                    // We convert the integer equation by factoring a 2^23 out of the exponent
                    //  value = (mantissa/2^23) * 2^23 * 2 ^ (1-127-23)
                    //  value = mantissa * 2 ^ (1-127-23)
                    // We have up to 23 bits of precision.
                    //   m = (mantissa)
                    //   e = (1-127-23)
                    mantissa = floatMantissa;
                    exponent = 1 - 127 - 23;
                    mantissaHighBitIdx = LogBase2(mantissa);
                    hasUnequalMargins = false;
                }

                var precision = formatOptions.Specifier == 0 ? -1 : formatOptions.Specifier;
                var bufferSize = Math.Max(SingleNumberBufferLength, precision + 1);

                var pDigits = stackalloc byte[bufferSize];

                var number = new NumberBuffer(NumberBufferKind.Float, pDigits, bufferSize, -1, floatUnion.IsNegative());

                uint numPrintDigits = Dragon4(mantissa,
                    exponent,
                    mantissaHighBitIdx,
                    hasUnequalMargins,
                    CutoffMode.TotalLength,
                    precision,
                    pDigits,
                    (uint)(bufferSize - 1),
                    out var printExponent);

                pDigits[numPrintDigits] = 0;
                number.DigitsCount = (int)numPrintDigits;
                number.Scale = printExponent + 1;

                if (precision == -1)
                {
                    // RJ_ASSERT(formatOptions.Kind == NumberFormatKind.General);

                    // For the roundtrip and general format specifiers, when returning the shortest roundtrippable
                    // string, we need to update the maximum number of digits to be the greater of number.DigitsCount
                    // or DoublePrecision. This ensures that we continue returning "pretty" strings for values with
                    // less digits. One example this fixes is "-60", which would otherwise be formatted as "-6E+01"
                    // since DigitsCount would be 1 and the formatter would almost immediately switch to scientific notation.
                    precision = Math.Max(number.DigitsCount, SinglePrecision);
                }

                FormatNumber(dest, ref destIndex, destLength, ref number, precision, formatOptions);
            }
        }

        /// <summary>
        /// Format a float 64-bit to a general format to the specified destination buffer.
        /// </summary>
        /// <param name="dest">Destination buffer.</param>
        /// <param name="destIndex">Current index in destination buffer.</param>
        /// <param name="destLength">Maximum length of destination buffer.</param>
        /// <param name="value">The float 64 value to format.</param>
        /// <param name="formatOptions">Formatting options.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe void ConvertDoubleToString(byte* dest, ref int destIndex, int destLength, double value, FormatOptions formatOptions)
        {
            // deconstruct the floating point value
            tFloatUnion64 floatUnion = default;
            floatUnion.m_floatingPoint = value;
            uint floatExponent = floatUnion.GetExponent();
            ulong floatMantissa = floatUnion.GetMantissa();

            // if this is a special value
            if (floatExponent == 0x7FF)
            {
                FormatInfinityNaN(dest, ref destIndex, destLength, floatMantissa, floatUnion.IsNegative(), formatOptions);
            }
            // else this is a number
            else
            {
                // factor the value into its parts
                ulong mantissa;
                int exponent;
                uint mantissaHighBitIdx;
                bool hasUnequalMargins;

                if (floatExponent != 0)
                {
                    // normal
                    // The floating point equation is:
                    //  value = (1 + mantissa/2^52) * 2 ^ (exponent-1023)
                    // We convert the integer equation by factoring a 2^52 out of the exponent
                    //  value = (1 + mantissa/2^52) * 2^52 * 2 ^ (exponent-1023-52)
                    //  value = (2^52 + mantissa) * 2 ^ (exponent-1023-52)
                    // Because of the implied 1 in front of the mantissa we have 53 bits of precision.
                    //   m = (2^52 + mantissa)
                    //   e = (exponent-1023+1-53)
                    mantissa = (1UL << 52) | floatMantissa;
                    exponent = (int)(floatExponent - 1023 - 52);
                    mantissaHighBitIdx = 52;
                    hasUnequalMargins = (floatExponent != 1) && (floatMantissa == 0);
                }
                else
                {
                    // subnormal
                    // The floating point equation is:
                    //  value = (mantissa/2^52) * 2 ^ (1-1023)
                    // We convert the integer equation by factoring a 2^52 out of the exponent
                    //  value = (mantissa/2^52) * 2^52 * 2 ^ (1-1023-52)
                    //  value = mantissa * 2 ^ (1-1023-52)
                    // We have up to 52 bits of precision.
                    //   m = (mantissa)
                    //   e = (1-1023-52)
                    mantissa = floatMantissa;
                    exponent = 1 - 1023 - 52;
                    mantissaHighBitIdx = LogBase2((uint)mantissa);
                    hasUnequalMargins = false;
                }

                var precision = formatOptions.Specifier == 0 ? -1 : formatOptions.Specifier;
                var bufferSize = Math.Max(DoubleNumberBufferLength, precision + 1);

                var pDigits = stackalloc byte[bufferSize];

                var number = new NumberBuffer(NumberBufferKind.Float, pDigits, bufferSize, -1, floatUnion.IsNegative());

                uint numPrintDigits = Dragon4(mantissa,
                    exponent,
                    mantissaHighBitIdx,
                    hasUnequalMargins,
                    CutoffMode.TotalLength,
                    precision,
                    pDigits,
                    (uint)(bufferSize - 1),
                    out var printExponent);
                pDigits[numPrintDigits] = 0;
                number.DigitsCount = (int)numPrintDigits;
                number.Scale = printExponent + 1;

                // If `precision == -1` then the number is roundtrippable.
                if (precision == -1)
                {
                    // RJ_ASSERT(formatOptions.Kind == NumberFormatKind.General);

                    // For the roundtrip and general format specifiers, when returning the shortest roundtrippable
                    // string, we need to update the maximum number of digits to be the greater of number.DigitsCount
                    // or DoublePrecision. This ensures that we continue returning "pretty" strings for values with
                    // less digits. One example this fixes is "-60", which would otherwise be formatted as "-6E+01"
                    // since DigitsCount would be 1 and the formatter would almost immediately switch to scientific notation.
                    precision = Math.Max(number.DigitsCount, DoublePrecision);
                }

                FormatNumber(dest, ref destIndex, destLength, ref number, precision, formatOptions);
            }
        }
    }
}
