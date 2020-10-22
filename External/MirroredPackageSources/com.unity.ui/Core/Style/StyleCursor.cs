using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Style value that can be either a <see cref="Cursor"/> or a <see cref="StyleKeyword"/>.
    /// </summary>
    public struct StyleCursor : IStyleValue<Cursor>, IEquatable<StyleCursor>
    {
        /// <summary>
        /// The <see cref="Cursor"/> value.
        /// </summary>
        public Cursor value
        {
            get { return m_Keyword == StyleKeyword.Undefined ? m_Value : default(Cursor); }
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
        /// Creates from either a <see cref="Cursor"/> or <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleCursor(Cursor v)
            : this(v, StyleKeyword.Undefined)
        {}

        /// <summary>
        /// Creates from either a <see cref="Cursor"/> or <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleCursor(StyleKeyword keyword)
            : this(default(Cursor), keyword)
        {}

        internal StyleCursor(Cursor v, StyleKeyword keyword)
        {
            m_Keyword = keyword;
            m_Value = v;
        }

        private Cursor m_Value;
        private StyleKeyword m_Keyword;

        public static bool operator==(StyleCursor lhs, StyleCursor rhs)
        {
            return lhs.m_Keyword == rhs.m_Keyword && lhs.m_Value == rhs.m_Value;
        }

        public static bool operator!=(StyleCursor lhs, StyleCursor rhs)
        {
            return !(lhs == rhs);
        }

        public static implicit operator StyleCursor(StyleKeyword keyword)
        {
            return new StyleCursor(keyword);
        }

        public static implicit operator StyleCursor(Cursor v)
        {
            return new StyleCursor(v);
        }

        public bool Equals(StyleCursor other)
        {
            return other == this;
        }

        public override bool Equals(object obj)
        {
            return obj is StyleCursor other && Equals(other);
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
