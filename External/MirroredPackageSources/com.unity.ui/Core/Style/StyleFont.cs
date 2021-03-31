using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Style value that can be either a <see cref="Font"/> or a <see cref="StyleKeyword"/>.
    /// </summary>
    public struct StyleFont : IStyleValue<Font>, IEquatable<StyleFont>
    {
        /// <summary>
        /// The <see cref="Font"/> value.
        /// </summary>
        public Font value
        {
            get { return m_Keyword == StyleKeyword.Undefined ? m_Value : null; }
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
        /// Creates from a <see cref="Font"/> or <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleFont(Font v)
            : this(v, StyleKeyword.Undefined)
        {}

        /// <summary>
        /// Creates from a <see cref="Font"/> or <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleFont(StyleKeyword keyword)
            : this(null, keyword)
        {}

        internal StyleFont(Font v, StyleKeyword keyword)
        {
            m_Keyword = keyword;
            m_Value = v;
        }

        private Font m_Value;
        private StyleKeyword m_Keyword;

        /// <undoc/>
        public static bool operator==(StyleFont lhs, StyleFont rhs)
        {
            return lhs.m_Keyword == rhs.m_Keyword && lhs.m_Value == rhs.m_Value;
        }

        /// <undoc/>
        public static bool operator!=(StyleFont lhs, StyleFont rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public static implicit operator StyleFont(StyleKeyword keyword)
        {
            return new StyleFont(keyword);
        }

        /// <undoc/>
        public static implicit operator StyleFont(Font v)
        {
            return new StyleFont(v);
        }

        /// <undoc/>
        public bool Equals(StyleFont other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is StyleFont other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((m_Value != null ? m_Value.GetHashCode() : 0) * 397) ^ (int)m_Keyword;
            }
        }

        public override string ToString()
        {
            return this.DebugString();
        }
    }
}
