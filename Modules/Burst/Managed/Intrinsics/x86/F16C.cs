// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics;

namespace Unity.Burst.Intrinsics
{
    public unsafe static partial class X86
    {
        /// <summary>
        /// F16C intrinsics
        /// </summary>
        public static class F16C
        {
            /// <summary>
            /// Evaluates to true at compile time if F16C intrinsics are supported.
            ///
            /// Burst ties F16C support to AVX2 support to simplify feature sets to support.
            /// </summary>
            public static bool IsF16CSupported { get { return Avx2.IsAvx2Supported; } }

            /// <summary>
            /// Converts a half (hiding in a ushort) to a float (hiding in a uint).
            /// </summary>
            /// <param name="h">The half to convert</param>
            /// <returns>The float result</returns>
            [DebuggerStepThrough]
            private static uint HalfToFloat(ushort h)
            {
                var signed = (h & 0x8000u) != 0;
                var exponent = (h >> 10) & 0x1fu;
                var mantissa = h & 0x3ffu;

                var result = signed ? 0x80000000u : 0u;

                if (!(exponent == 0 && mantissa == 0))
                {
                    // Denormal (converts to normalized)
                    if (exponent == 0)
                    {
                        // Adjust mantissa so it's normalized (and keep track of exponent adjustment)
                        exponent = -1;
                        do
                        {
                            exponent++;
                            mantissa <<= 1;
                        } while ((mantissa & 0x400) == 0);

                        result |= (uint)((127 - 15 - exponent) << 23);

                        // Have to re-mask the mantissa here because we've been shifting bits up.
                        result |= (mantissa & 0x3ff) << 13;
                    }
                    else
                    {
                        var isInfOrNan = exponent == 0x1f;
                        result |= (uint)(isInfOrNan ? 255 : (127 - 15 + exponent) << 23);
                        result |= mantissa << 13;
                    }
                }

                return result;
            }

            /// <summary>
            /// Convert packed half-precision (16-bit) floating-point elements in a to packed single-precision (32-bit) floating-point elements, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** vcvtph2ps xmm, xmm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cvtph_ps(v128 a)
            {
                return new v128(HalfToFloat(a.UShort0), HalfToFloat(a.UShort1), HalfToFloat(a.UShort2), HalfToFloat(a.UShort3));
            }

            /// <summary>
            /// Convert packed half-precision (16-bit) floating-point elements in a to packed single-precision (32-bit) floating-point elements, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** vcvtph2ps ymm, xmm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_cvtph_ps(v128 a)
            {
                return new v256(HalfToFloat(a.UShort0), HalfToFloat(a.UShort1), HalfToFloat(a.UShort2), HalfToFloat(a.UShort3), HalfToFloat(a.UShort4), HalfToFloat(a.UShort5), HalfToFloat(a.UShort6), HalfToFloat(a.UShort7));
            }

            // Using ftp://ftp.fox-toolkit.org/pub/fasthalffloatconversion.pdf
            private static readonly ushort[] BaseTable =
            {
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0001, 0x0002, 0x0004, 0x0008, 0x0010, 0x0020, 0x0040, 0x0080, 0x0100,
                0x0200, 0x0400, 0x0800, 0x0C00, 0x1000, 0x1400, 0x1800, 0x1C00, 0x2000, 0x2400, 0x2800, 0x2C00, 0x3000, 0x3400, 0x3800, 0x3C00,
                0x4000, 0x4400, 0x4800, 0x4C00, 0x5000, 0x5400, 0x5800, 0x5C00, 0x6000, 0x6400, 0x6800, 0x6C00, 0x7000, 0x7400, 0x7800, 0x7C00,
                0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00,
                0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00,
                0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00,
                0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00,
                0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00,
                0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00,
                0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00, 0x7C00,
                0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000,
                0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000,
                0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000,
                0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000,
                0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000,
                0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000,
                0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8000, 0x8001, 0x8002, 0x8004, 0x8008, 0x8010, 0x8020, 0x8040, 0x8080, 0x8100,
                0x8200, 0x8400, 0x8800, 0x8C00, 0x9000, 0x9400, 0x9800, 0x9C00, 0xA000, 0xA400, 0xA800, 0xAC00, 0xB000, 0xB400, 0xB800, 0xBC00,
                0xC000, 0xC400, 0xC800, 0xCC00, 0xD000, 0xD400, 0xD800, 0xDC00, 0xE000, 0xE400, 0xE800, 0xEC00, 0xF000, 0xF400, 0xF800, 0xFC00,
                0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00,
                0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00,
                0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00,
                0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00,
                0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00,
                0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00,
                0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00, 0xFC00,
            };

            private static readonly sbyte[] ShiftTable =
            {
                24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
                24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
                24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
                24, 24, 24, 24, 24, 24, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
                13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
                24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
                24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
                24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 13,
                24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
                24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
                24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
                24, 24, 24, 24, 24, 24, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
                13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
                24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
                24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
                24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 13,
            };

            /// <summary>
            /// Converts a float (hiding in a uint) to a half (hiding in a ushort).
            /// </summary>
            /// <param name="f">The float to convert</param>
			/// <param name="rounding">Rounding mode</param>
            /// <returns>The half result</returns>
            [DebuggerStepThrough]
            private static ushort FloatToHalf(uint f, int rounding)
            {
                var exponentAndSign = f >> 23;
                var shift = ShiftTable[exponentAndSign];

                var result = (uint)(BaseTable[exponentAndSign] + (ushort)((f & 0x7FFFFFu) >> shift));

                // Check if the result is not Inf or NaN.
                var isFinite = (result & 0x7C00) != 0x7C00;
                var isNegative = (result & 0x8000) != 0;

                if (rounding == (int)RoundingMode.FROUND_NINT_NOEXC)
                {
                    var fWithRoundingBitPreserved = (f & 0x7FFFFFu) >> (shift - 1);

                    if ((exponentAndSign & 0xFF) == 102)
                    {
                        result++;
                    }
                    if (isFinite && ((fWithRoundingBitPreserved & 0x1u) != 0))
                    {
                        result++;
                    }
                }
                else if (rounding == (int)RoundingMode.FROUND_TRUNC_NOEXC)
                {
                    if (!isFinite)
                    {
                        result -= (uint)(~shift & 0x1);
                    }
                }
                else if (rounding == (int)RoundingMode.FROUND_CEIL_NOEXC)
                {
                    if (isFinite && !isNegative)
                    {
                        if ((exponentAndSign <= 102) && (exponentAndSign != 0))
                        {
                            result++;
                        }
                        else if ((f & 0x7FFFFFu & ((1u << shift) - 1u)) != 0)
                        {
                            result++;
                        } 
                    }

                    var resultIsNegativeInf = (result == 0xFC00);
                    var inputIsNotNegativeInfOrNan = (exponentAndSign != 0x1FF);

                    if (resultIsNegativeInf && inputIsNotNegativeInfOrNan)
                    {
                        result--;
                    }
                }
                else if (rounding == (int)RoundingMode.FROUND_FLOOR_NOEXC)
                {
                    if (isFinite && isNegative)
                    {
                        if ((exponentAndSign <= 358) && (exponentAndSign != 256))
                        {
                            result++;
                        }
                        else if ((f & 0x7FFFFFu & ((1u << shift) - 1u)) != 0)
                        {
                            result++;
                        }
                    }

                    var resultIsPositiveInf = (result == 0x7C00);
                    var inputIsNotPositiveInfOrNan = (exponentAndSign != 0xFF);

                    if (resultIsPositiveInf && inputIsNotPositiveInfOrNan)
                    {
                        result--;
                    }
                }

                return (ushort)result;
            }

            /// <summary>
            /// Convert packed single-precision (32-bit) floating-point elements in a to packed half-precision (16-bit) floating-point elements, and store the results in dst.
            ///
            /// Rounding is done according to the rounding parameter, which can be one of:
            /// </summary>
            /// <remarks>
            /// **** cvtps2ph xmm, xmm, imm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="rounding">Rounding mode</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cvtps_ph(v128 a, int rounding)
            {
                if (rounding == (int)RoundingMode.FROUND_RINT_NOEXC)
                {
                    switch (MXCSR & MXCSRBits.RoundingControlMask)
                    {
                        case MXCSRBits.RoundToNearest:
                            rounding = (int)RoundingMode.FROUND_NINT_NOEXC;
                            break;
                        case MXCSRBits.RoundDown:
                            rounding = (int)RoundingMode.FROUND_FLOOR_NOEXC;
                            break;
                        case MXCSRBits.RoundUp:
                            rounding = (int)RoundingMode.FROUND_CEIL_NOEXC;
                            break;
                        case MXCSRBits.RoundTowardZero:
                            rounding = (int)RoundingMode.FROUND_TRUNC_NOEXC;
                            break;
                    }
                }

                return new v128(FloatToHalf(a.UInt0, rounding), FloatToHalf(a.UInt1, rounding), FloatToHalf(a.UInt2, rounding), FloatToHalf(a.UInt3, rounding), 0, 0, 0, 0);
            }

            /// <summary>
            /// Convert packed single-precision (32-bit) floating-point elements in a to packed half-precision (16-bit) floating-point elements, and store the results in dst.
            ///
            /// Rounding is done according to the rounding parameter, which can be one of:
            /// </summary>
            /// <remarks>
            /// **** cvtps2ph xmm, ymm, imm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="rounding">Rounding mode</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 mm256_cvtps_ph(v256 a, int rounding)
            {
                if (rounding == (int)RoundingMode.FROUND_RINT_NOEXC)
                {
                    switch (MXCSR & MXCSRBits.RoundingControlMask)
                    {
                        case MXCSRBits.RoundToNearest:
                            rounding = (int)RoundingMode.FROUND_NINT_NOEXC;
                            break;
                        case MXCSRBits.RoundDown:
                            rounding = (int)RoundingMode.FROUND_FLOOR_NOEXC;
                            break;
                        case MXCSRBits.RoundUp:
                            rounding = (int)RoundingMode.FROUND_CEIL_NOEXC;
                            break;
                        case MXCSRBits.RoundTowardZero:
                            rounding = (int)RoundingMode.FROUND_TRUNC_NOEXC;
                            break;
                    }
                }

                return new v128(FloatToHalf(a.UInt0, rounding), FloatToHalf(a.UInt1, rounding), FloatToHalf(a.UInt2, rounding), FloatToHalf(a.UInt3, rounding), FloatToHalf(a.UInt4, rounding), FloatToHalf(a.UInt5, rounding), FloatToHalf(a.UInt6, rounding), FloatToHalf(a.UInt7, rounding));
            }
        }
    }
}
