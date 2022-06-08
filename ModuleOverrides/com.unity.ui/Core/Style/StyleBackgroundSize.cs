// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Represents a style value that can be either a <see cref="BackgroundSize"/> or a <see cref="StyleKeyword"/>.
    /// </summary>
    public struct StyleBackgroundSize : IStyleValue<BackgroundSize>, IEquatable<StyleBackgroundSize>
    {
        /// <summary>
        /// The <see cref="BackgroundSize"/> value.
        /// </summary>
        public BackgroundSize value
        {
            get { return m_Keyword == StyleKeyword.Undefined ? m_Value : default(BackgroundSize);  }
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
        /// Creates a new StyleBackgroundSize from either a <see cref="BackgroundSize"/> or a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleBackgroundSize(BackgroundSize v)
            : this(v, StyleKeyword.Undefined)
        {}

        /// <summary>
        /// Creates  a new StyleBackgroundSize from either a <see cref="BackgroundSize"/> or a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleBackgroundSize(StyleKeyword keyword)
            : this(default(BackgroundSize), keyword)
        {}

        internal StyleBackgroundSize(BackgroundSize v, StyleKeyword keyword)
        {
            m_Keyword = keyword;
            m_Value = v;
        }

        private BackgroundSize m_Value;
        private StyleKeyword m_Keyword;

        /// <undoc/>
        public static bool operator==(StyleBackgroundSize lhs, StyleBackgroundSize rhs)
        {
            return lhs.m_Keyword == rhs.m_Keyword && lhs.m_Value == rhs.m_Value;
        }

        /// <undoc/>
        public static bool operator!=(StyleBackgroundSize lhs, StyleBackgroundSize rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public static implicit operator StyleBackgroundSize(StyleKeyword keyword)
        {
            return new StyleBackgroundSize(keyword);
        }

        /// <undoc/>
        public static implicit operator StyleBackgroundSize(BackgroundSize v)
        {
            return new StyleBackgroundSize(v);
        }

        /// <undoc/>
        public bool Equals(StyleBackgroundSize other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is StyleBackgroundSize other && Equals(other);
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
