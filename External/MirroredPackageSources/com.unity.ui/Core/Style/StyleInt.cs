using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Style value that can be either an integer or a <see cref="StyleKeyword"/>.
    /// </summary>
    public struct StyleInt : IStyleValue<int>, IEquatable<StyleInt>
    {
        /// <summary>
        /// The integer value.
        /// </summary>
        public int value
        {
            get { return m_Keyword == StyleKeyword.Undefined ? m_Value : default(int); }
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
        /// Creates from either an integer or <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleInt(int v)
            : this(v, StyleKeyword.Undefined)
        {}

        /// <summary>
        /// Creates from either an integer or <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleInt(StyleKeyword keyword)
            : this(default(int), keyword)
        {}

        internal StyleInt(int v, StyleKeyword keyword)
        {
            m_Keyword = keyword;
            m_Value = v;
        }

        private int m_Value;
        private StyleKeyword m_Keyword;

        public static bool operator==(StyleInt lhs, StyleInt rhs)
        {
            return lhs.m_Keyword == rhs.m_Keyword && lhs.m_Value == rhs.m_Value;
        }

        public static bool operator!=(StyleInt lhs, StyleInt rhs)
        {
            return !(lhs == rhs);
        }

        public static implicit operator StyleInt(StyleKeyword keyword)
        {
            return new StyleInt(keyword);
        }

        public static implicit operator StyleInt(int v)
        {
            return new StyleInt(v);
        }

        public bool Equals(StyleInt other)
        {
            return other == this;
        }

        public override bool Equals(object obj)
        {
            return obj is StyleInt other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (m_Value * 397) ^ (int)m_Keyword;
            }
        }

        public override string ToString()
        {
            return this.DebugString();
        }
    }
}
