// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using System.Diagnostics;

namespace Unity.Collections
{
    /// <summary>
    /// An interface for a sequence of UTF-8 encoded text.
    /// </summary>
    public interface IUTF8Bytes
    {
        /// <summary>
        /// Whether this IUTF8Bytes is empty.
        /// </summary>
        /// <value>True if this IUTF8Bytes is empty.</value>
        bool IsEmpty { get; }

        /// <summary>
        /// Returns a pointer to the content of this IUTF8Bytes.
        /// </summary>
        /// <remarks>The pointer may point to stack memory.</remarks>
        /// <returns>A pointer to the content of this IUTF8Bytes.</returns>
        unsafe byte* GetUnsafePtr();

        /// <summary>
        /// Attempt to set the length in bytes of this IUTF8Bytes's content buffer.
        /// </summary>
        /// <param name="newLength">The new length in bytes of the IUTF8Bytes's content buffer.</param>
        /// <param name="clearOptions">Whether any bytes added should be zeroed out.</param>
        /// <returns>True if the new length is valid.</returns>
        bool TryResize(int newLength, NativeArrayOptions clearOptions = NativeArrayOptions.ClearMemory);
    }

    [GenerateTestsForBurstCompatibility]
    internal unsafe static class FixedStringUtils
    {
        [StructLayout(LayoutKind.Explicit)]
        internal struct UintFloatUnion
        {
            [FieldOffset(0)]
            public uint uintValue;
            [FieldOffset(0)]
            public float floatValue;
        }

        internal static ParseError Base10ToBase2(ref float output, ulong mantissa10, int exponent10)
        {
            if (mantissa10 == 0)
            {
                output = 0.0f;
                return ParseError.None;
            }
            if (exponent10 == 0)
            {
                output = mantissa10;
                return ParseError.None;
            }
            var exponent2 = exponent10;
            var mantissa2 = mantissa10;
            while (exponent10 > 0)
            {
                while ((mantissa2 & 0xe000000000000000U) != 0)
                {
                    mantissa2 >>= 1;
                    ++exponent2;
                }
                mantissa2 *= 5;
                --exponent10;
            }
            while (exponent10 < 0)
            {
                while ((mantissa2 & 0x8000000000000000U) == 0)
                {
                    mantissa2 <<= 1;
                    --exponent2;
                }
                mantissa2 /= 5;
                ++exponent10;
            }
            // TODO: implement math.ldexpf (which presumably handles denormals (i don't))
            UintFloatUnion ufu = new UintFloatUnion();
            ufu.floatValue = mantissa2;
            var e = (int)((ufu.uintValue >> 23) & 0xFFU) - 127;
            e += exponent2;
            if (e > 128)
                return ParseError.Overflow;
            if (e < -127)
                return ParseError.Underflow;
            ufu.uintValue = (ufu.uintValue & ~(0xFFU << 23)) | ((uint)(e + 127) << 23);
            output = ufu.floatValue;
            return ParseError.None;
        }

        internal static void Base2ToBase10(ref ulong mantissa10, ref int exponent10, float input)
        {
            UintFloatUnion ufu = new UintFloatUnion();
            ufu.floatValue = input;
            if (ufu.uintValue == 0)
            {
                mantissa10 = 0;
                exponent10 = 0;
                return;
            }
            var mantissa2 = (ufu.uintValue & ((1 << 23) - 1)) | (1 << 23);
            var exponent2 = (int)(ufu.uintValue >> 23) - 127 - 23;
            mantissa10 = mantissa2;
            exponent10 = exponent2;
            if (exponent2 > 0)
            {
                while (exponent2 > 0)
                {
                    // denormalize mantissa10 as much as you can, to minimize loss when doing /5 below.
                    while (mantissa10 <= UInt64.MaxValue / 10)
                    {
                        mantissa10 *= 10;
                        --exponent10;
                    }
                    mantissa10 /= 5;
                    --exponent2;
                }
            }
            if (exponent2 < 0)
            {
                while (exponent2 < 0)
                {
                    // normalize mantissa10 just as much as you need, in order to make the *5 below not overflow.
                    while (mantissa10 > UInt64.MaxValue / 5)
                    {
                        mantissa10 /= 10;
                        ++exponent10;
                    }
                    mantissa10 *= 5;
                    ++exponent2;
                }
            }
            // normalize mantissa10
            while (mantissa10 > 9999999U || mantissa10 % 10 == 0)
            {
                mantissa10 = (mantissa10 + (mantissa10 < 100000000U ? 5u : 0u)) / 10;
                ++exponent10;
            }
        }
    }
}
