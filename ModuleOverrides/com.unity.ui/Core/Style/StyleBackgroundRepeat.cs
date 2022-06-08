// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Represents a style value that can be either a <see cref="BackgroundRepeat"/> or a <see cref="StyleKeyword"/>.
    /// </summary>
    public struct StyleBackgroundRepeat : IStyleValue<BackgroundRepeat>, IEquatable<StyleBackgroundRepeat>
    {
        /// <summary>
        /// The <see cref="BackgroundRepeat"/> value.
        /// </summary>
        public BackgroundRepeat value
        {
            get { return m_Keyword == StyleKeyword.Undefined ? m_Value : default(BackgroundRepeat);  }
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
        /// Creates a new StyleBackgroundRepeat from either a <see cref="BackgroundRepeat"/> or a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleBackgroundRepeat(BackgroundRepeat v)
            : this(v, StyleKeyword.Undefined)
        {}

        /// <summary>
        /// Creates  a new StyleBackgroundRepeat from either a <see cref="BackgroundRepeat"/> or a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleBackgroundRepeat(StyleKeyword keyword)
            : this(default(BackgroundRepeat), keyword)
        {}

        internal StyleBackgroundRepeat(BackgroundRepeat v, StyleKeyword keyword)
        {
            m_Keyword = keyword;
            m_Value = v;
        }

        private BackgroundRepeat m_Value;
        private StyleKeyword m_Keyword;

        /// <undoc/>
        public static bool operator==(StyleBackgroundRepeat lhs, StyleBackgroundRepeat rhs)
        {
            return lhs.m_Keyword == rhs.m_Keyword && lhs.m_Value == rhs.m_Value;
        }

        /// <undoc/>
        public static bool operator!=(StyleBackgroundRepeat lhs, StyleBackgroundRepeat rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public static implicit operator StyleBackgroundRepeat(StyleKeyword keyword)
        {
            return new StyleBackgroundRepeat(keyword);
        }

        /// <undoc/>
        public static implicit operator StyleBackgroundRepeat(BackgroundRepeat v)
        {
            return new StyleBackgroundRepeat(v);
        }

        /// <undoc/>
        public bool Equals(StyleBackgroundRepeat other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is StyleBackgroundRepeat other && Equals(other);
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
