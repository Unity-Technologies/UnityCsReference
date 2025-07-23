// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Represents a ratio value, denotes the proportion between two unitless values.
    /// </summary>
    public struct StyleRatio : IStyleValue<Ratio>, IEquatable<StyleRatio>
    {
        /// <summary>
        /// The float value.
        /// </summary>
        public Ratio value
        {
            get { return m_Keyword == StyleKeyword.Undefined ? m_Value : float.NaN; }
            set
            {
                m_Value = value;
                m_Keyword = StyleKeyword.Undefined;
            }
        }

        /// <summary>
        /// The style keyword.
        /// </summary>
        public StyleKeyword keyword
        {
            get { return m_Keyword; }
            set
            {
                m_Keyword = value;
                m_Value = float.NaN;
            }
        }

        /// <summary>
        /// Creates a new Ratio from a float.
        /// </summary>
        public StyleRatio(Ratio value)
            : this(value, StyleKeyword.Undefined)
        {}

        /// <summary>
        /// Creates a new Ratio from a Keywords.
        /// </summary>
        public StyleRatio(StyleKeyword keyword)
            : this(float.NaN, keyword)
        { }

        internal StyleRatio(Ratio value, StyleKeyword keyword)
        {
            m_Keyword = keyword;
            m_Value = value;
        }

        /// <summary>
        /// Create a special ratio value that indicate no specific ratio should be used.
        /// </summary>
        public static StyleRatio Auto()
        {
            return new StyleRatio(float.NaN, StyleKeyword.Auto);
        }

        private Ratio m_Value;
        private StyleKeyword m_Keyword;

        internal bool IsAuto() => m_Keyword == StyleKeyword.Auto;

        /// <undoc/>
        public static implicit operator StyleRatio(float value)
        {
            return new StyleRatio(value);
        }

        /// <undoc/>
        public static implicit operator float(StyleRatio value)
        {
            return value.value;
        }

        /// <undoc/>
        public static implicit operator StyleRatio(Ratio value)
        {
            return new(value);
        }

        /// <undoc/>
        public static implicit operator Ratio(StyleRatio value)
        {
            return value.value;
        }

        /// <undoc/>
        public static implicit operator StyleKeyword(StyleRatio value)
        {
            return value.keyword;
        }

        /// <undoc/>
        public static implicit operator StyleRatio(StyleKeyword value)
        {
            return new StyleRatio(value);
        }

        /// <undoc/>
        public static bool operator==(StyleRatio lhs, StyleRatio rhs)
        {
            return lhs.m_Keyword == rhs.m_Keyword && lhs.m_Value == rhs.m_Value;
        }

        /// <undoc/>
        public static bool operator!=(StyleRatio lhs, StyleRatio rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public bool Equals(StyleRatio other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is StyleRatio other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (m_Value.GetHashCode() * 793);
            }
        }

        public override string ToString()
        {
            return IsAuto() ? StyleValueKeywordExtension.ToUssString(StyleValueKeyword.Auto)
                : m_Value.value.ToString(CultureInfo.InvariantCulture.NumberFormat);
        }
    }
}
