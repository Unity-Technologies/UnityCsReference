// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Style value that can be either a <see cref="BoxShadow"/> or a <see cref="StyleKeyword"/>.
    /// </summary>
    [Serializable]
    public struct StyleBoxShadow : IStyleValue<BoxShadow>, IEquatable<StyleBoxShadow>
    {
        /// <summary>
        /// The <see cref="BoxShadow"/> value.
        /// </summary>
        public BoxShadow value
        {
            get { return m_Keyword == StyleKeyword.Undefined ? m_Value : default(BoxShadow); }
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
        /// Creates from either a <see cref="BoxShadow"/> or <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleBoxShadow(BoxShadow v)
            : this(v, StyleKeyword.Undefined)
        {}

        /// <summary>
        /// Creates from either a <see cref="BoxShadow"/> or <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleBoxShadow(StyleKeyword keyword)
            : this(default(BoxShadow), keyword)
        {}

        internal StyleBoxShadow(BoxShadow v, StyleKeyword keyword)
        {
            m_Keyword = keyword;
            m_Value = v;
        }

        [SerializeField]
        private StyleKeyword m_Keyword;
        [SerializeField]
        private BoxShadow m_Value;

        /// <undoc/>
        public static bool operator==(StyleBoxShadow lhs, StyleBoxShadow rhs)
        {
            return lhs.m_Keyword == rhs.m_Keyword && lhs.m_Value == rhs.m_Value;
        }

        /// <undoc/>
        public static bool operator!=(StyleBoxShadow lhs, StyleBoxShadow rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public static implicit operator StyleBoxShadow(StyleKeyword keyword)
        {
            return new StyleBoxShadow(keyword);
        }

        /// <undoc/>
        public static implicit operator StyleBoxShadow(BoxShadow v)
        {
            return new StyleBoxShadow(v);
        }

        /// <undoc/>
        public bool Equals(StyleBoxShadow other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            if (!(obj is StyleBoxShadow))
            {
                return false;
            }

            var v = (StyleBoxShadow)obj;
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
