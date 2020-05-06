using System;
using System.Globalization;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Style value that can be either an enum or a <see cref="StyleKeyword"/>.
    /// </summary>
    public struct StyleEnum<T> : IStyleValue<T>, IEquatable<StyleEnum<T>> where T : struct, IConvertible
    {
        /// <summary>
        /// The style value.
        /// </summary>
        public T value
        {
            get { return m_Keyword == StyleKeyword.Undefined ? m_Value : default(T); }
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
        /// Creates from either an enum or a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleEnum(T v)
            : this(v, StyleKeyword.Undefined)
        {}

        /// <summary>
        /// Creates from either an enum or a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleEnum(StyleKeyword keyword)
            : this(default(T), keyword)
        {}

        internal StyleEnum(T v, StyleKeyword keyword)
        {
            m_Keyword = keyword;
            m_Value = v;
        }

        private StyleKeyword m_Keyword;
        private T m_Value;

        public static bool operator==(StyleEnum<T> lhs, StyleEnum<T> rhs)
        {
            return lhs.m_Keyword == rhs.m_Keyword && UnsafeUtility.EnumEquals(lhs.m_Value, rhs.m_Value);
        }

        public static bool operator!=(StyleEnum<T> lhs, StyleEnum<T> rhs)
        {
            return !(lhs == rhs);
        }

        public static implicit operator StyleEnum<T>(StyleKeyword keyword)
        {
            return new StyleEnum<T>(keyword);
        }

        public static implicit operator StyleEnum<T>(T v)
        {
            return new StyleEnum<T>(v);
        }

        public bool Equals(StyleEnum<T> other)
        {
            return other == this;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is StyleEnum<T>))
            {
                return false;
            }

            var v = (StyleEnum<T>)obj;
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
