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
        /// Creates from a <see cref="StylePropertyName"/> from a string.
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

        /// <undoc/>
        public static bool IsNullOrEmpty(StylePropertyName propertyName) { return propertyName.id == StylePropertyId.Unknown; }

        /// <undoc/>
        public static bool operator==(StylePropertyName lhs, StylePropertyName rhs)
        {
            return lhs.id == rhs.id;
        }

        /// <undoc/>
        public static bool operator!=(StylePropertyName lhs, StylePropertyName rhs)
        {
            return lhs.id != rhs.id;
        }

        /// <undoc/>
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
