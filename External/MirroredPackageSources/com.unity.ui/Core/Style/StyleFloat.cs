using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Style value that can be either a float or a <see cref="StyleKeyword"/>.
    /// </summary>
    public struct StyleFloat : IStyleValue<float>, IEquatable<StyleFloat>
    {
        /// <summary>
        /// The float value.
        /// </summary>
        public float value
        {
            get { return m_Keyword == StyleKeyword.Undefined ? m_Value : default(float); }
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
        /// Creates from either a float or a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleFloat(float v)
            : this(v, StyleKeyword.Undefined)
        {}

        /// <summary>
        /// Creates from either a float or a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleFloat(StyleKeyword keyword)
            : this(default(float), keyword)
        {}

        internal StyleFloat(float v, StyleKeyword keyword)
        {
            m_Keyword = keyword;
            m_Value = v;
        }

        private float m_Value;
        private StyleKeyword m_Keyword;

        /// <undoc/>
        public static bool operator==(StyleFloat lhs, StyleFloat rhs)
        {
            return lhs.m_Keyword == rhs.m_Keyword && lhs.m_Value == rhs.m_Value;
        }

        /// <undoc/>
        public static bool operator!=(StyleFloat lhs, StyleFloat rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public static implicit operator StyleFloat(StyleKeyword keyword)
        {
            return new StyleFloat(keyword);
        }

        /// <undoc/>
        public static implicit operator StyleFloat(float v)
        {
            return new StyleFloat(v);
        }

        /// <undoc/>
        public bool Equals(StyleFloat other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is StyleFloat other && Equals(other);
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
