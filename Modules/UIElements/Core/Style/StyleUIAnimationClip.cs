// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Style value that can be either a <see cref="UIAnimationClip"/> or a <see cref="StyleKeyword"/>.
    /// </summary>
    public struct StyleUIAnimationClip : IStyleValue<UIAnimationClip>, IEquatable<StyleUIAnimationClip>
    {
        /// <summary>
        /// The <see cref="UIAnimationClip"/> value.
        /// </summary>
        public UIAnimationClip value
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
        /// Creates from a <see cref="UIAnimationClip"/> or <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleUIAnimationClip(UIAnimationClip v)
            : this(v, StyleKeyword.Undefined)
        { }

        /// <summary>
        /// Creates from a <see cref="UIAnimationClip"/> or <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleUIAnimationClip(StyleKeyword keyword)
            : this(null, keyword)
        { }

        internal StyleUIAnimationClip(UIAnimationClip v, StyleKeyword keyword)
        {
            m_Keyword = keyword;
            m_Value = v;
        }

        private UIAnimationClip m_Value;
        private StyleKeyword m_Keyword;

        /// <undoc/>
        public static bool operator ==(StyleUIAnimationClip lhs, StyleUIAnimationClip rhs)
        {
            return lhs.m_Keyword == rhs.m_Keyword && lhs.m_Value == rhs.m_Value;
        }

        /// <undoc/>
        public static bool operator !=(StyleUIAnimationClip lhs, StyleUIAnimationClip rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public static implicit operator StyleUIAnimationClip(StyleKeyword keyword)
        {
            return new StyleUIAnimationClip(keyword);
        }

        /// <undoc/>
        public static implicit operator StyleUIAnimationClip(UIAnimationClip v)
        {
            return new StyleUIAnimationClip(v);
        }

        /// <undoc/>
        public bool Equals(StyleUIAnimationClip other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is StyleUIAnimationClip other && Equals(other);
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
