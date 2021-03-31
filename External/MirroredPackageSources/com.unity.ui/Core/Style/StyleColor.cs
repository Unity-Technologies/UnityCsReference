using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Style value that can be either a <see cref="Color"/> or a <see cref="StyleKeyword"/>.
    /// </summary>
    public struct StyleColor : IStyleValue<Color>, IEquatable<StyleColor>
    {
        /// <summary>
        /// The <see cref="Color"/> value.
        /// </summary>
        public Color value
        {
            get { return m_Keyword == StyleKeyword.Undefined ? m_Value : Color.clear; }
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
        /// Creates from either a <see cref="Color"/> or <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleColor(Color v)
            : this(v, StyleKeyword.Undefined)
        {}

        /// <summary>
        /// Creates from either a <see cref="Color"/> or <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleColor(StyleKeyword keyword)
            : this(Color.clear, keyword)
        {}

        internal StyleColor(Color v, StyleKeyword keyword)
        {
            m_Keyword = keyword;
            m_Value = v;
        }

        private Color m_Value;
        private StyleKeyword m_Keyword;

        /// <undoc/>
        public static bool operator==(StyleColor lhs, StyleColor rhs)
        {
            return lhs.m_Keyword == rhs.m_Keyword && lhs.m_Value == rhs.m_Value;
        }

        /// <undoc/>
        public static bool operator!=(StyleColor lhs, StyleColor rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public static bool operator==(StyleColor lhs, Color rhs)
        {
            var styleColor = new StyleColor(rhs);
            return lhs == styleColor;
        }

        /// <undoc/>
        public static bool operator!=(StyleColor lhs, Color rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public static implicit operator StyleColor(StyleKeyword keyword)
        {
            return new StyleColor(keyword);
        }

        /// <undoc/>
        public static implicit operator StyleColor(Color v)
        {
            return new StyleColor(v);
        }

        /// <undoc/>
        public bool Equals(StyleColor other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is StyleColor other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (m_Value.GetHashCode() * 397) ^ (int)m_Keyword;
            }
        }

        public override string ToString()
        {
            return this.DebugString();
        }
    }
}
