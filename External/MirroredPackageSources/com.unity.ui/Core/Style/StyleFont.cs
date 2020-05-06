using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.UIElements.StyleSheets;

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

        internal StyleFont(GCHandle gcHandle, StyleKeyword keyword)
            : this(gcHandle.IsAllocated ? gcHandle.Target as Font : null, keyword)
        {}

        internal StyleFont(Font v, StyleKeyword keyword)
        {
            m_Keyword = keyword;
            m_Value = v;
        }

        private StyleKeyword m_Keyword;
        private Font m_Value;

        public static bool operator==(StyleFont lhs, StyleFont rhs)
        {
            return lhs.m_Keyword == rhs.m_Keyword && lhs.m_Value == rhs.m_Value;
        }

        public static bool operator!=(StyleFont lhs, StyleFont rhs)
        {
            return !(lhs == rhs);
        }

        public static implicit operator StyleFont(StyleKeyword keyword)
        {
            return new StyleFont(keyword);
        }

        public static implicit operator StyleFont(Font v)
        {
            return new StyleFont(v);
        }

        public bool Equals(StyleFont other)
        {
            return other == this;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is StyleFont))
            {
                return false;
            }

            var v = (StyleFont)obj;
            return v == this;
        }

        public override int GetHashCode()
        {
            var hashCode = 917506989;
            hashCode = hashCode * -1521134295 + m_Keyword.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<Font>.Default.GetHashCode(m_Value);
            return hashCode;
        }

        public override string ToString()
        {
            return this.DebugString();
        }
    }
}
