// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Represents a style value that can be either a <see cref="BackgroundPosition"/> or a <see cref="StyleKeyword"/>.
    /// </summary>
    public struct StyleBackgroundPosition : IStyleValue<BackgroundPosition>, IEquatable<StyleBackgroundPosition>
    {
        /// <summary>
        /// The <see cref="BackgroundPosition"/> value.
        /// </summary>
        public BackgroundPosition value
        {
            get { return m_Keyword == StyleKeyword.Undefined ? m_Value : default(BackgroundPosition);  }
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
        /// Creates a new StyleBackgroundPosition from either a <see cref="BackgroundPosition"/> or a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleBackgroundPosition(BackgroundPosition v)
            : this(v, StyleKeyword.Undefined)
        {}

        /// <summary>
        /// Creates  a new StyleBackgroundPosition from either a <see cref="BackgroundPosition"/> or a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleBackgroundPosition(StyleKeyword keyword)
            : this(default(BackgroundPosition), keyword)
        {}

        internal StyleBackgroundPosition(BackgroundPosition v, StyleKeyword keyword)
        {
            m_Keyword = keyword;
            m_Value = v;
        }

        private BackgroundPosition m_Value;
        private StyleKeyword m_Keyword;

        /// <undoc/>
        public static bool operator==(StyleBackgroundPosition lhs, StyleBackgroundPosition rhs)
        {
            return lhs.m_Keyword == rhs.m_Keyword && lhs.m_Value == rhs.m_Value;
        }

        /// <undoc/>
        public static bool operator!=(StyleBackgroundPosition lhs, StyleBackgroundPosition rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public static implicit operator StyleBackgroundPosition(StyleKeyword keyword)
        {
            return new StyleBackgroundPosition(keyword);
        }

        /// <undoc/>
        public static implicit operator StyleBackgroundPosition(BackgroundPosition v)
        {
            return new StyleBackgroundPosition(v);
        }

        /// <undoc/>
        public bool Equals(StyleBackgroundPosition other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is StyleBackgroundPosition other && Equals(other);
        }

        /// <undoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return (m_Value.GetHashCode() * 397) ^ (int)m_Keyword;
            }
        }

        /// <undoc/>
        public override string ToString()
        {
            return this.DebugString();
        }
    }
}
