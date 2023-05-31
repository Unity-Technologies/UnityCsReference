// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Properties;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// The structure that keeps track of the <see cref="Button"/> states inside a <see cref="ToggleButtonGroup"/>.
    /// </summary>
    [Serializable]
    public struct ToggleButtonGroupState : IEquatable<ToggleButtonGroupState>, IComparable<ToggleButtonGroupState>
    {
        private const int k_MaxLength = 64;
        private ulong m_Data;
        private int m_Length;

        /// <summary>
        /// Constructs a ToggleButtonGroupState.
        /// </summary>
        /// <param name="optionsBitMask">A bit value where each bit represents the binary state of the corresponding button.</param>
        /// <param name="length">The number of toggle button options.</param>
        /// <exception cref="ArgumentOutOfRangeException">If the length exceeds the max length of 64 or the value provided is negative.</exception>
        /// <remarks>
        /// The maximum number of toggle button options allowed is 64. Exceeding this number will throw an ArgumentOutOfRangeException.
        /// </remarks>
        public ToggleButtonGroupState(ulong optionsBitMask, int length)
        {
            if (length is < 0 or > k_MaxLength)
                throw new ArgumentOutOfRangeException(nameof(length), $"length of {length} should be greater than or equal to 0 and less than or equal to {k_MaxLength}.");

            m_Data = optionsBitMask;
            m_Length = length;
            ResetOptions(m_Length);
        }

        /// <summary>
        /// Returns the number of toggle button options available.
        /// </summary>
        public int length => m_Length;

        /// <summary>
        /// The option based on the index.
        /// </summary>
        /// <param name="index">The index of the option.</param>
        /// <exception cref="ArgumentOutOfRangeException">If the index exceeds the length assigned or is below 0.</exception>
        public bool this[int index]
        {
            get
            {
                if (index < 0 || index >= m_Length)
                    throw new ArgumentOutOfRangeException(nameof(index), $"index of {index} should be in the range of 0 and ({m_Length} - 1) inclusively.");

                var bit = 1ul << index;
                return (m_Data & bit) == bit;
            }
            set
            {
                if (index < 0 || index >= m_Length)
                    throw new ArgumentOutOfRangeException(nameof(index), $"index of {index} should be in the range of 0 and ({m_Length} - 1) inclusively.");

                var option = 1ul << index;
                if (value)
                    m_Data |= option;
                else
                    m_Data &= ~option;
            }
        }

        /// <summary>
        /// Retrieves a Span of integers containing the active options as indices.
        /// </summary>
        /// <param name="activeOptionsIndices">A Span of type integers with the allocated size to hold the number of active indices.</param>
        /// <exception cref="ArgumentException">If the Span length is smaller than the length of the ToggleButtonGroupState.</exception>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// public void HandleActiveOptions()
        /// {
        ///     var value = myToggleButtonGroup.value;
        ///     var options = value.GetActiveOptions(stackalloc int[value.length]);
        ///     foreach (option in options)
        ///     {
        ///         // handle option
        ///     }
        /// }
        /// ]]>
        /// </code>
        /// </example>
        public Span<int> GetActiveOptions(in Span<int> activeOptionsIndices)
        {
            if (activeOptionsIndices.Length < m_Length)
                throw new ArgumentException($"indices' length ({activeOptionsIndices.Length}) should be equal to or greater than the ToggleButtonGroupState's length ({m_Length}).");

            var count = 0;
            for (var i = 0; i < m_Length; ++i)
            {
                if (!this[i]) continue;

                activeOptionsIndices[count] = i;
                count++;
            }

            return activeOptionsIndices.Slice(0, count);
        }

        /// <summary>
        /// Retrieves a Span of integers containing the inactive options as indices.
        /// </summary>
        /// <param name="inactiveOptionsIndices">A Span of type integers with the allocated size to hold the number of inactive indices.</param>
        /// <exception cref="ArgumentException">If the Span length is smaller than the length of the ToggleButtonGroupState.</exception>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// public void HandleInactiveOptions()
        /// {
        ///     var value = myToggleButtonGroup.value;
        ///     var options = value.GetInactiveOptions(stackalloc int[value.length]);
        ///     foreach (option in options)
        ///     {
        ///         // handle option
        ///     }
        /// }
        /// ]]>
        /// </code>
        /// </example>
        public Span<int> GetInactiveOptions(in Span<int> inactiveOptionsIndices)
        {
            if (inactiveOptionsIndices.Length < m_Length)
                throw new ArgumentException($"indices' length ({inactiveOptionsIndices.Length}) should be equal to or greater than the ToggleButtonGroupState's length ({m_Length}).");

            var count = 0;
            for (var i = 0; i < m_Length; ++i)
            {
                if (this[i]) continue;

                inactiveOptionsIndices[count] = i;
                count++;
            }

            return inactiveOptionsIndices.Slice(0, count);
        }

        /// <summary>
        /// Sets all the available options to active.
        /// </summary>
        public void SetAllOptions()
        {
            m_Data = ulong.MaxValue;
            ResetOptions(m_Length);
        }

        /// <summary>
        /// Resets the states of the toggle buttons.
        /// </summary>
        /// <remarks>This will clear all the selected options.</remarks>
        public void ResetAllOptions()
        {
            m_Data = 0;
        }

        /// <summary>
        /// Toggles all the available options' state.
        /// </summary>
        /// <remarks>
        /// Calling this method will make all the active toggle buttons to be inactive and all the inactive toggle
        /// buttons to be active.
        /// </remarks>
        public void ToggleAllOptions()
        {
            m_Data = ~m_Data;
            ResetOptions(m_Length);
        }

        /// <summary>
        /// Helps generate a ToggleButtonGroupState based on a list of booleans.
        /// </summary>
        /// <param name="options">A list of booleans that is used to generated a ToggleButtonGroupState.</param>
        /// <returns>a ToggleButtonGroupState created by the list of booleans.</returns>
        public static ToggleButtonGroupState CreateFromOptions(IList<bool> options)
        {
            var optionsCount = options.Count;
            var toggleButtonGroupState = new ToggleButtonGroupState(0, optionsCount);

            for (var i = 0; i < optionsCount; ++i)
                toggleButtonGroupState[i] = options[i];

            return toggleButtonGroupState;
        }

        /// <summary>
        /// Creates a ToggleButtonGroupState based on a FlagsAttribute enum type.
        /// </summary>
        /// <param name="options">The default Enum Flag value.</param>
        /// <param name="length">The number of options.</param>
        /// <typeparam name="T">A type of FlagsAttribute.</typeparam>
        /// <returns>A ToggleButtonGroupState based on the EnumFlag passed as an argument.</returns>
        /// <exception cref="ArgumentException">If the Enum type is not a flag enum type.</exception>
        public static ToggleButtonGroupState FromEnumFlags<T>(T options, int length = -1)
            where T : Enum
        {
            if (!TypeTraits<T>.IsEnumFlags)
                throw new ArgumentException($"Enum type {nameof(T)} is not a flag enum type.");

            var underlyingType = Enum.GetUnderlyingType(typeof(T));

            // Detect length from the enum directly
            if (length == -1)
            {
                length = Type.GetTypeCode(underlyingType) switch
                {
                    TypeCode.Byte => 8,
                    TypeCode.SByte => 8,
                    TypeCode.UInt16 => 16,
                    TypeCode.Int16 => 16,
                    TypeCode.UInt32 => 32,
                    TypeCode.Int32 => 32,
                    TypeCode.Int64 => 64,
                    TypeCode.UInt64 => 64,
                    _ => 0
                };
            }

            return new ToggleButtonGroupState((ulong) UnsafeUtility.As<T, int>(ref options), length);
        }

        /// <summary>
        /// Synchronizes the internal data with the a ToggleButtonGroupState from a FlagsAttribute.
        /// </summary>
        /// <param name="options">The option set to be synced against.</param>
        /// <param name="acceptsLengthMismatch">The ability to synchronize two option sets of different sizes.</param>
        /// <typeparam name="T">A Type of FlagsAttribute.</typeparam>
        /// <returns>A flag enum type.</returns>
        /// <exception cref="ArgumentException">If the Enum type is not a flag enum type.</exception>
        /// <exception cref="ArgumentException">If acceptsLengthMismatch is false and the options does not match the enum's length.</exception>
        public static T ToEnumFlags<T>(ToggleButtonGroupState options, bool acceptsLengthMismatch = true)
            where T : Enum
        {
            if (!TypeTraits<T>.IsEnumFlags)
                throw new ArgumentException($"Enum type {nameof(T)} is not a flag enum type.");

            var underlyingType = Enum.GetUnderlyingType(typeof(T));
            var enumLength = Type.GetTypeCode(underlyingType) switch
            {
                TypeCode.Byte => 8,
                TypeCode.SByte => 8,
                TypeCode.UInt16 => 16,
                TypeCode.Int16 => 16,
                TypeCode.UInt32 => 32,
                TypeCode.Int32 => 32,
                TypeCode.Int64 => 64,
                TypeCode.UInt64 => 64,
                _ => -1
            };

            if (!acceptsLengthMismatch && options.m_Length != enumLength)
                throw new ArgumentException($"Cannot sync to enum flag since the {nameof(ToggleButtonGroupState)} has a different amount of options.");

            return (T) Enum.Parse(typeof(T), options.m_Data.ToString());
        }

        /// <summary>
        /// Compares two ToggleButtonGroupState.
        /// </summary>
        /// <param name="other">The option set to be compared against.</param>
        /// <returns>True if both option sets are the same, otherwise returns false.</returns>
        public int CompareTo(ToggleButtonGroupState other)
        {
            return other == this ? 1 : -1;
        }

        /// <summary>
        /// Compares two ToggleButtonGroupState of flag enum types.
        /// </summary>
        /// <param name="options">The ToggleButtonGroupState to be compared with.</param>
        /// <param name="value">The value of a given flag enum.</param>
        /// <typeparam name="T">A Type of FlagsAttribute.</typeparam>
        /// <returns>True if both options sets are the same, otherwise false.</returns>
        /// <exception cref="ArgumentException">If the Enum type is not a flag enum type.</exception>
        public static bool Compare<T>(ToggleButtonGroupState options, T value)
            where T : Enum
        {
            if (!TypeTraits<T>.IsEnumFlags)
                throw new ArgumentException($"Enum type {nameof(T)} is not a flag enum type.");

            var v = (ulong) UnsafeUtility.As<T, int>(ref value);
            return options.m_Data == v;
        }

        private void ResetOptions(int startingIndex)
        {
            for (var i = startingIndex; i < k_MaxLength; ++i)
            {
                var option = 1ul << i;
                m_Data &= ~option;
            }
        }

        /// <summary>
        /// Checks if both of the ToggleButtonGroupState are the same.
        /// </summary>
        /// <param name="lhs">The first option set to be compared against.</param>
        /// <param name="rhs">The second option set to be compared against.</param>
        /// <returns>True if both option sets are the same, otherwise returns false.</returns>
        public static bool operator ==(ToggleButtonGroupState lhs, ToggleButtonGroupState rhs)
        {
            return lhs.Equals(rhs);
        }

        /// <summary>
        /// Checks if both of the ToggleButtonGroupState are not the same.
        /// </summary>
        /// <param name="lhs">The first option set to be compared against.</param>
        /// <param name="rhs">The second option set to be compared against.</param>
        /// <returns>True if both option sets are not the same, otherwise returns false.</returns>
        public static bool operator !=(ToggleButtonGroupState lhs, ToggleButtonGroupState rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Checks if a given ToggleButtonGroupState matches with the current one.
        /// </summary>
        /// <param name="other">A ToggleButtonGroupState to be compared against.</param>
        /// <returns>True if both option has the same data and number of options, otherwise returns false.</returns>
        public bool Equals(ToggleButtonGroupState other)
        {
            return m_Data == other.m_Data && m_Length == other.m_Length;
        }

        /// <summary>
        /// Compares the the current option set with an Object.
        /// </summary>
        /// <param name="obj">An object to be compared against.</param>
        /// <returns>True if both option sets are not the same, otherwise returns false.</returns>
        public override bool Equals(object obj)
        {
            return obj is ToggleButtonGroupState other && Equals(other);
        }

        /// <summary>
        /// Get the hashcode of this ToggleButtonGroupState.
        /// </summary>
        /// <returns>The hashcode of this ToggleButtonGroupState.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(m_Data, m_Length);
        }
    }
}

