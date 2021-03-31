using System;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Style value that can be either a <see cref="TextShadow"/> or a <see cref="StyleKeyword"/>.
    /// </summary>
    public struct StyleTextShadow : IStyleValue<TextShadow>, IEquatable<StyleTextShadow>
    {
        /// <summary>
        /// The <see cref="TextShadow"/> value.
        /// </summary>
        public TextShadow value
        {
            get { return m_Keyword == StyleKeyword.Undefined ? m_Value : default(TextShadow); }
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
        /// Creates from either a <see cref="TextShadow"/> or <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleTextShadow(TextShadow v)
            : this(v, StyleKeyword.Undefined)
        {}

        /// <summary>
        /// Creates from either a <see cref="TextShadow"/> or <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleTextShadow(StyleKeyword keyword)
            : this(default(TextShadow), keyword)
        {}

        internal StyleTextShadow(TextShadow v, StyleKeyword keyword)
        {
            m_Keyword = keyword;
            m_Value = v;
        }

        private StyleKeyword m_Keyword;
        private TextShadow m_Value;

        /// <undoc/>
        public static bool operator==(StyleTextShadow lhs, StyleTextShadow rhs)
        {
            return lhs.m_Keyword == rhs.m_Keyword && lhs.m_Value == rhs.m_Value;
        }

        /// <undoc/>
        public static bool operator!=(StyleTextShadow lhs, StyleTextShadow rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public static implicit operator StyleTextShadow(StyleKeyword keyword)
        {
            return new StyleTextShadow(keyword);
        }

        /// <undoc/>
        public static implicit operator StyleTextShadow(TextShadow v)
        {
            return new StyleTextShadow(v);
        }

        /// <undoc/>
        public bool Equals(StyleTextShadow other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            if (!(obj is StyleTextShadow))
            {
                return false;
            }

            var v = (StyleTextShadow)obj;
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
