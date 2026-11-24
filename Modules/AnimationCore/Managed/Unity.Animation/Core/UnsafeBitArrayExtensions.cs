// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Animations
{
    [GenerateTestsForBurstCompatibility]
    static internal class UnsafeBitArrayExtensions
    {
        const int k_SizeMultiple = 64;

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ValidateLengthsAreEqual(in UnsafeBitArray.ReadOnly source, in UnsafeBitArray.ReadOnly destination)
        {
            if (source.Length != destination.Length)
                throw new ArgumentException($"UnsafeBitArray must be of the same length: {source.Length} vs {destination.Length}.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ValidateLengthIsMultipleOf64(in UnsafeBitArray.ReadOnly bits)
        {
            if ((bits.Length & (k_SizeMultiple - 1)) != 0)
                throw new ArgumentException($"UnsafeBitArray length must be a multiple of 64: {bits.Length}.");
        }

        /// <summary>
        /// Set all bits to desired boolean value.
        /// </summary>
        /// <param name="value">Value of bits to set.</param>
        internal static void SetBits(this ref UnsafeBitArray bits, bool value)
        {
            bits.SetBits(0, value, bits.Length);
        }

        /// <summary>
        /// Copy all bits from source.
        /// </summary>
        /// <param name="src">Bits to copy from.</param>
        /// <remarks>
        /// Source bit array should be of same length.
        /// </remarks>
        internal static void CopyFrom(this ref UnsafeBitArray bits, ref UnsafeBitArray src)
        {
            ValidateLengthsAreEqual(bits.AsReadOnly(), src.AsReadOnly());

            bits.Copy(0, ref src, 0, bits.Length);
        }

        /// <summary>
        /// OR with all bits from source.
        /// </summary>
        /// <param name="src">Input bits.</param>
        /// <remarks>
        /// This method only works with UnsafeBitArray of 64 bit multiples.
        /// Source bit array should be of same length.
        /// </remarks>
        internal static void OrBits64(this ref UnsafeBitArray bits, in UnsafeBitArray.ReadOnly src)
        {
            ValidateLengthsAreEqual(bits.AsReadOnly(), src);
            ValidateLengthIsMultipleOf64(src);

            var length = src.Length;
            for (int i = 0; i < length; i += k_SizeMultiple)
            {
                var value = bits.GetBits(i, k_SizeMultiple) | src.GetBits(i, k_SizeMultiple);
                bits.SetBits(i, value, k_SizeMultiple);
            }
        }

        /// <summary>
        /// OR all bits from lhs with rhs and store result. Both lhs and rhs parameters are not modified.
        /// </summary>
        /// <param name="lhs">Input bits.</param>
        /// <param name="rhs">Input bits.</param>
        /// <remarks>
        /// This method only works with UnsafeBitArray of 64 bit multiples and does not modify lhs or rhs.
        /// All UnsafeBitArray must be of same size.
        /// </remarks>
        internal static void OrBits64(this ref UnsafeBitArray bits, in UnsafeBitArray.ReadOnly lhs, in UnsafeBitArray.ReadOnly rhs)
        {
            ValidateLengthsAreEqual(bits.AsReadOnly(), lhs);
            ValidateLengthsAreEqual(lhs, rhs);
            ValidateLengthIsMultipleOf64(lhs);

            var length = lhs.Length;
            for (int i = 0; i < length; i += k_SizeMultiple)
            {
                var value = lhs.GetBits(i, k_SizeMultiple) | rhs.GetBits(i, k_SizeMultiple);
                bits.SetBits(i, value, k_SizeMultiple);
            }
        }

        /// <summary>
        /// Store lhs OR (rhs AND channels). Both lhs and rhs parameters are not modified.
        /// </summary>
        /// <param name="lhs">Input bits that are always stored.</param>
        /// <param name="rhs">Input bits. Only the bits that are set in channels are stored.</param>
        /// <param name="channels">The bits of rhs to store.</param>
        /// <remarks>
        /// This method only works with UnsafeBitArray of 64 bit multiples and does not modify lhs or rhs.
        /// All UnsafeBitArray must be of same size.
        /// </remarks>
        internal static void OrBits64Where(this ref UnsafeBitArray bits, in UnsafeBitArray.ReadOnly lhs, in UnsafeBitArray.ReadOnly rhs, in UnsafeBitArray.ReadOnly channels)
        {
            ValidateLengthsAreEqual(bits.AsReadOnly(), lhs);
            ValidateLengthsAreEqual(lhs, rhs);
            ValidateLengthsAreEqual(lhs, channels);
            ValidateLengthIsMultipleOf64(lhs);

            var length = lhs.Length;
            for (int i = 0; i < length; i += k_SizeMultiple)
            {
                var value = lhs.GetBits(i, k_SizeMultiple) | (rhs.GetBits(i, k_SizeMultiple) & channels.GetBits(i, k_SizeMultiple));
                bits.SetBits(i, value, k_SizeMultiple);
            }
        }

        /// <summary>
        /// AND with all bits from source.
        /// </summary>
        /// <param name="src">Input bits.</param>
        /// <remarks>
        /// This method only works with UnsafeBitArray of 64 bit multiples.
        /// Source bit array should be of same length.
        /// </remarks>
        internal static void AndBits64(this ref UnsafeBitArray bits, in UnsafeBitArray.ReadOnly src)
        {
            ValidateLengthsAreEqual(bits.AsReadOnly(), src);
            ValidateLengthIsMultipleOf64(src);

            var length = src.Length;
            for (int i = 0; i < length; i += k_SizeMultiple)
            {
                var value = bits.GetBits(i, k_SizeMultiple) & src.GetBits(i, k_SizeMultiple);
                bits.SetBits(i, value, k_SizeMultiple);
            }
        }

        /// <summary>
        /// OR all bits from lhs with rhs and store result. Both lhs and rhs parameters are not modified.
        /// </summary>
        /// <param name="lhs">Input bits.</param>
        /// <param name="rhs">Input bits.</param>
        /// <remarks>
        /// This method only works with UnsafeBitArray of 64 bit multiples and does not modify lhs or rhs.
        /// All UnsafeBitArray must be of same size.
        /// </remarks>
        internal static void AndBits64(this ref UnsafeBitArray bits, in UnsafeBitArray.ReadOnly lhs, in UnsafeBitArray.ReadOnly rhs)
        {
            ValidateLengthsAreEqual(bits.AsReadOnly(), lhs);
            ValidateLengthsAreEqual(lhs, rhs);
            ValidateLengthIsMultipleOf64(lhs);

            var length = lhs.Length;
            for (int i = 0; i < length; i += k_SizeMultiple)
            {
                var value = lhs.GetBits(i, k_SizeMultiple) & rhs.GetBits(i, k_SizeMultiple);
                bits.SetBits(i, value, k_SizeMultiple);
            }
        }

        /// <summary>
        /// Invert all bits.
        /// </summary>
        /// <remarks>
        /// This method only works with UnsafeBitArray of 64 bit multiples.
        /// </remarks>
        internal static void InvertBits64(this ref UnsafeBitArray bits)
        {
            ValidateLengthIsMultipleOf64(bits.AsReadOnly());

            var length = bits.Length;
            for (int i = 0; i < length; i += k_SizeMultiple)
            {
                bits.SetBits(i, ~bits.GetBits(i, k_SizeMultiple), k_SizeMultiple);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CopyFrom(this ref UnsafeBitArray bits, in ChannelMask src)
        {
            var bitsRo = bits.AsReadOnly();
            ValidateLengthsAreEqual(bitsRo, src.m_Masks);
            ValidateLengthIsMultipleOf64(src.m_Masks);

            unsafe { UnsafeUtility.MemCpy(bitsRo.Ptr, src.m_Masks.Ptr, bitsRo.Length / 8); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void OrBits64(this ref UnsafeBitArray bits, in ChannelMask src) =>
            OrBits64(ref bits, src.m_Masks);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void OrBits64(this ref UnsafeBitArray bits, in ChannelMask lhs, in ChannelMask rhs) =>
            OrBits64(ref bits, lhs.m_Masks, rhs.m_Masks);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AndBits64(this ref UnsafeBitArray bits, in ChannelMask src) =>
            AndBits64(ref bits, src.m_Masks);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AndBits64(this ref UnsafeBitArray bits, in ChannelMask lhs, in ChannelMask rhs) =>
            AndBits64(ref bits, lhs.m_Masks, rhs.m_Masks);
    }
}
