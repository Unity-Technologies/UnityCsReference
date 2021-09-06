// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Style value that can be either a <see cref="Scale"/> or a <see cref="StyleKeyword"/>.
    /// </summary>
    public struct StyleScale : IStyleValue<Scale>, IEquatable<StyleScale>
    {
        /// <summary>
        /// The <see cref="Scale"/> value.
        /// </summary>
        public Scale value
        {
            get { return m_Keyword == StyleKeyword.Undefined ? m_Value : default(Scale); }
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
        /// Creates a new StyleScale from a <see cref="Scale"/>.
        /// </summary>
        public StyleScale(Scale v)
            : this(v, StyleKeyword.Undefined)
        {}

        /// <summary>
        /// Creates a new StyleScale from a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleScale(StyleKeyword keyword)
            : this(default(Scale), keyword)
        {}

        /// <summary>
        /// Creates a new StyleScale from a <see cref="Vector2"/>.
        /// </summary>
        public StyleScale(Vector2 scale)
            : this(new Scale(scale))
        {}

        internal StyleScale(Scale v, StyleKeyword keyword)
        {
            m_Keyword = keyword;
            m_Value = v;
        }

        private Scale m_Value;
        private StyleKeyword m_Keyword;

        /// <undoc/>
        public static implicit operator StyleScale(Vector2 scale)
        {
            return new Scale(scale);
        }

        /// <undoc/>
        public static bool operator==(StyleScale lhs, StyleScale rhs)
        {
            return lhs.m_Keyword == rhs.m_Keyword && lhs.m_Value == rhs.m_Value;
        }

        /// <undoc/>
        public static bool operator!=(StyleScale lhs, StyleScale rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public static implicit operator StyleScale(StyleKeyword keyword)
        {
            return new StyleScale(keyword);
        }

        /// <undoc/>
        public static implicit operator StyleScale(Scale v)
        {
            return new StyleScale(v);
        }

        /// <undoc/>
        public bool Equals(StyleScale other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is StyleScale other && Equals(other);
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
