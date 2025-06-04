// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Style value that can be either a <see cref="TextAutoSize"/> or a <see cref="StyleKeyword"/>.
    /// </summary>
    public struct StyleTextAutoSize : IStyleValue<TextAutoSize>, IEquatable<StyleTextAutoSize>
    {
        private StyleKeyword m_Keyword;
        private TextAutoSize m_Value;

        /// <summary>
        /// The <see cref="TextAutoSize"/> value.
        /// </summary>
        public TextAutoSize value
        {
            get { return m_Keyword == StyleKeyword.Undefined ? m_Value : default(TextAutoSize); }
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
            set { m_Keyword = value; }
        }

        /// <summary>
        /// Creates a new StyleTextAutoSize from a TextAutoSize value.
        /// </summary>
        public StyleTextAutoSize(TextAutoSize v)
            : this(v, StyleKeyword.Undefined)
        {}

        /// <summary>
        /// Creates a new StyleTextAutoSize from a StyleKeyword.
        /// </summary>
        public StyleTextAutoSize(StyleKeyword keyword)
            : this(default(TextAutoSize), keyword)
        {}

        internal StyleTextAutoSize(TextAutoSize v, StyleKeyword keyword)
        {
            m_Value = v;
            m_Keyword = keyword;
        }

        /// <undoc/>
        public static bool operator==(StyleTextAutoSize lhs, StyleTextAutoSize rhs)
        {
            return lhs.m_Keyword == rhs.m_Keyword && lhs.m_Value.Equals(rhs.m_Value);
        }

        /// <undoc/>
        public static bool operator!=(StyleTextAutoSize lhs, StyleTextAutoSize rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public static implicit operator StyleTextAutoSize(StyleKeyword keyword)
        {
            return new StyleTextAutoSize(keyword);
        }

        /// <undoc/>
        public static implicit operator StyleTextAutoSize(TextAutoSize v)
        {
            return new StyleTextAutoSize(v);
        }

        /// <undoc/>
        public bool Equals(StyleTextAutoSize other)
        {
            return this == other;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is StyleTextAutoSize other && Equals(other);
        }

        /// <undoc/>
        public override int GetHashCode()
        {
            var hashCode = 917506989;
            hashCode = hashCode * -1521134295 + m_Keyword.GetHashCode();
            hashCode = hashCode * -1521134295 + m_Value.GetHashCode();
            return hashCode;
        }

        /// <undoc/>
        public override string ToString()
        {
            return this.DebugString();
        }
    }
}
