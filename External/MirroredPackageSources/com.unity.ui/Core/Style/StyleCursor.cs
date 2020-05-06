using System;
using UnityEngine.UIElements.StyleSheets;

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

        private StyleKeyword m_Keyword;
        private Cursor m_Value;

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
            if (!(obj is StyleCursor))
            {
                return false;
            }

            var v = (StyleCursor)obj;
            return v == this;
        }

        public override int GetHashCode()
        {
            var hashCode = 917506989;
            hashCode = hashCode * -1521134295 + m_Keyword.GetHashCode();
            hashCode = hashCode * -1521134295 + m_Value.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return this.DebugString();
        }
    }
}
