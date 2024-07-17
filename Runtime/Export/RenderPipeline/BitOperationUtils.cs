// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UnityEngine
{
    internal static class BitOperationUtils
    {
        /// <summary>
        /// Counts the number of set bits (ones) in the given integer mask.
        /// </summary>
        /// <param name="mask">The mask for which to count the set bits.</param>
        /// <returns>The number of bits set to one in the mask.</returns>
        internal static int CountBits(int mask) => CountBits(unchecked((uint) mask));

        /// <summary>
        /// Counts the total number of bits set to 1 in the provided unsigned integer mask.
        /// </summary>
        /// <param name="mask">The mask whose bits are to be counted.</param>
        /// <returns>The count of bits set to 1.</returns>
        internal static int CountBits(uint mask)
        {
            var count = 0u;
            while (mask != 0u)
            {
                // Increment count if the least significant bit is set
                count += mask & 1u;
                // Shift the mask to the right by one bit
                mask >>= 1;
            }
            return unchecked((int) count);
        }

        /// <summary>
        /// Checks if a specified value has any matching bits with a given mask and fits within a range defined by the number of bits.
        /// </summary>
        /// <param name="value">The value to check for matching bits within the masked range.</param>
        /// <param name="mask">The mask that defines which bits to consider.</param>
        /// <param name="bitCount">The number of least significant bits to consider in the range check.</param>
        /// <returns>True if any bit of the specified value matches with the mask within the range specified by bitCount; otherwise, false.</returns>
        static bool IsValueWithinMaskedBitsRange(uint value, uint mask, int bitCount)
        {
            return AnyBitMatch(mask, value) && IsValueSmallerOrEqualThanIndex(value, BitCountToIndex(bitCount));
        }

        /// <summary>
        /// Modifies the given mask by incorporating the values provided in the array, considering only the number of bits specified by bitCount.
        /// </summary>
        /// <param name="mask">The initial mask to be modified.</param>
        /// <param name="values">The collection of integer values to consider for modification of the mask.</param>
        /// <param name="bitCount">The number of significant bits to consider, defaulting to 32.</param>
        /// <returns>The modified mask after incorporating the specified values within the given bit count.</returns>
        internal static uint ModifyMaskByValuesArrayAndBitCount(uint mask, IEnumerable<int> values, int bitCount = 32)
        {
            AssertBitCount(bitCount);

            uint calculatedMask = 0;
            foreach (var value in values)
            {
                var definedValue = unchecked((uint)value);
                if (IsValueWithinMaskedBitsRange(definedValue, mask, bitCount))
                    calculatedMask += definedValue;
            }

            return calculatedMask;
        }

        /// <summary>
        /// Determines whether all specified values have their corresponding bits set within a given mask and within a bit count limit.
        /// </summary>
        /// <param name="mask">The mask to be checked for set bits.</param>
        /// <param name="values">An IEnumerable of integers representing the values to check within the mask.</param>
        /// <param name="bitCount">The maximum number of bits to consider, defaults to 32 if not specified.</param>
        /// <returns>True if all the specified bits are set within the mask and bit count limit; otherwise, false.</returns>
        internal static bool AreAllBitsSetForValues(uint mask, IEnumerable<int> values, int bitCount = 32)
        {
            AssertBitCount(bitCount);

            foreach (var value in values)
            {
                var definedValue = unchecked((uint)value);
                var isAnyBitMatch = AnyBitMatch(mask, definedValue);
                if (!isAnyBitMatch || IsValueBiggerOrEqualThanIndex(definedValue, BitCountToIndex(bitCount)))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Converts a bit index into a corresponding uint value with only that bit set.
        /// </summary>
        /// <param name="index">The index of the bit to set, ranging from 0 to 31.</param>
        /// <returns>A uint value with the bit at the specified index set to 1.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint IndexToValue(int index)
        {
            AssertIndex(index);
            return 1u << index;
        }

        /// <summary>
        /// Determines whether the given unsigned integer value is less than the value represented by a bit at the specified index.
        /// </summary>
        /// <param name="value">The value to compare.</param>
        /// <param name="index">The bit index used to generate a comparison value.</param>
        /// <returns>True if the value is smaller than the bit value at the specified index; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsValueSmallerThanIndex(uint value, int index)
        {
            AssertIndex(index);
            return value < IndexToValue(index);
        }

        /// <summary>
        /// Determines whether the given unsigned integer value is greater than the value represented by the bit at the specified index.
        /// </summary>
        /// <param name="value">The value to compare.</param>
        /// <param name="index">The bit index used to generate a comparison value.</param>
        /// <returns>True if the value is greater than the bit value at the specified index; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsValueBiggerThanIndex(uint value, int index)
        {
            AssertIndex(index);
            return value > IndexToValue(index);
        }

        /// <summary>
        /// Determines whether the given unsigned integer value is less than or equal to the value represented by the bit at the specified index.
        /// </summary>
        /// <param name="value">The value to compare.</param>
        /// <param name="index">The bit index used to generate a comparison value.</param>
        /// <returns>True if the value is less than or equal to the bit value at the specified index; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsValueSmallerOrEqualThanIndex(uint value, int index)
        {
            AssertIndex(index);
            return value <= IndexToValue(index);
        }

        /// <summary>
        /// Determines whether the given unsigned integer value is greater than or equal to the value represented by the bit at the specified index.
        /// </summary>
        /// <param name="value">The value to compare.</param>
        /// <param name="index">The bit index used to generate a comparison value.</param>
        /// <returns>True if the value is greater than or equal to the bit value at the specified index; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsValueBiggerOrEqualThanIndex(uint value, int index)
        {
            AssertIndex(index);
            return value >= IndexToValue(index);
        }

        /// <summary>
        /// Checks if any bit in the given mask matches the corresponding bit in the provided value.
        /// </summary>
        /// <param name="mask">The mask used for comparison.</param>
        /// <param name="value">The value to compare against the mask.</param>
        /// <returns>True if at least one matching bit is found; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool AnyBitMatch(uint mask, uint value)
        {
            return (mask & value) != 0;
        }

        /// <summary>
        /// Converts a bit count to a zero-based index, with the assertion that the bit count is within a valid range.
        /// </summary>
        /// <param name="bitCount">The bit count to convert, which should be between 1 and 32.</param>
        /// <returns>The corresponding zero-based index.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int BitCountToIndex(int bitCount)
        {
            AssertBitCount(bitCount);
            return bitCount - 1;
        }

        /// <summary>
        /// Confirms that the provided bit count is within the valid range of 1 to 32 inclusive, throwing an assertion if not.
        /// </summary>
        /// <param name="bitCount">The bit count to check for validity.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void AssertBitCount(int bitCount)
        {
            Debug.Assert(bitCount is >= 1 and <= 32, "Bit count must be between 1 and 32.");
        }

        /// <summary>
        /// Checks whether the provided index is within the valid range of 0 to 31 inclusive.
        /// </summary>
        /// <param name="index">The index to validate.</param>
        /// <returns>True if the index is valid; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void AssertIndex(int index)
        {
            Debug.Assert(index is >= 0 and <= 31, "Index must be between 0 and 31.");
        }
    }
}
