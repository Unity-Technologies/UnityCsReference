// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Defines the name of a style property.
    /// </summary>
    public struct StylePropertyName : IEquatable<StylePropertyName>
    {
        internal StylePropertyId id { get; }
        private string name { get; }

        internal static StylePropertyId StylePropertyIdFromString(string name)
        {
            if (StylePropertyUtil.s_NameToId.TryGetValue(name, out StylePropertyId id))
            {
                return id;
            }
            return StylePropertyId.Unknown;
        }

        internal StylePropertyName(StylePropertyId stylePropertyId)
        {
            id = stylePropertyId;
            this.name = default;
            if (StylePropertyUtil.s_IdToName.TryGetValue(stylePropertyId, out string name))
            {
                this.name = name;
            }
        }

        /// <summary>
        /// Initializes and returns an instance of <see cref="StylePropertyName"/> from a string.
        /// </summary>
        public StylePropertyName(string name)
        {
            id = StylePropertyIdFromString(name);
            this.name = default;
            if (id != StylePropertyId.Unknown)
            {
                this.name = name;
            }
        }

        /// <summary>
        /// Checks if the StylePropertyName is null or empty.
        /// </summary>
        /// <param name="propertyName">StylePropertyName you want to check.</param>
        /// <returns>True if propertyName is invalid. False otherwise.</returns>
        public static bool IsNullOrEmpty(StylePropertyName propertyName) { return propertyName.id == StylePropertyId.Unknown; }

        /// <summary>
        /// Determines if the StylePropertyNames are equal.
        /// </summary>
        /// <param name="lhs">First StylePropertyName.</param>
        /// <param name="rhs">Second StylePropertyName.</param>
        /// <returns>True if both StylePropertyNames are equal. False otherwise.</returns>
        public static bool operator==(StylePropertyName lhs, StylePropertyName rhs)
        {
            return lhs.id == rhs.id;
        }

        /// <summary>
        /// Determines if the StylePropertyNames are not equal.
        /// </summary>
        /// <param name="lhs">First StylePropertyName.</param>
        /// <param name="rhs">Second StylePropertyName.</param>
        /// <returns>True if the StylePropertyNames are not equal. False otherwise.</returns>
        public static bool operator!=(StylePropertyName lhs, StylePropertyName rhs)
        {
            return lhs.id != rhs.id;
        }

        /// <summary>
        /// Implicit string operator.
        /// </summary>
        /// <param name="name">Name of the property you want to create a new StylePropertyName with.</param>
        public static implicit operator StylePropertyName(string name)
        {
            return new StylePropertyName(name);
        }

        public override int GetHashCode()
        {
            return (int)id;
        }

        public override bool Equals(object other)
        {
            return other is StylePropertyName && Equals((StylePropertyName)other);
        }

        public bool Equals(StylePropertyName other)
        {
            return this == other;
        }

        public override string ToString()
        {
            return name;
        }
    }
}
