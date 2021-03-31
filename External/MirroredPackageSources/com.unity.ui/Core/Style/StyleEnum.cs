using System;
using Unity.Collections.LowLevel.Unsafe;

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

        private T m_Value;
        private StyleKeyword m_Keyword;

        /// <undoc/>
        public static bool operator==(StyleEnum<T> lhs, StyleEnum<T> rhs)
        {
            return lhs.m_Keyword == rhs.m_Keyword && UnsafeUtility.EnumEquals(lhs.m_Value, rhs.m_Value);
        }

        /// <undoc/>
        public static bool operator!=(StyleEnum<T> lhs, StyleEnum<T> rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public static implicit operator StyleEnum<T>(StyleKeyword keyword)
        {
            return new StyleEnum<T>(keyword);
        }

        /// <undoc/>
        public static implicit operator StyleEnum<T>(T v)
        {
            return new StyleEnum<T>(v);
        }

        /// <undoc/>
        public bool Equals(StyleEnum<T> other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is StyleEnum<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (UnsafeUtility.EnumToInt(m_Value) * 397) ^ (int)m_Keyword;
            }
        }

        public override string ToString()
        {
            return this.DebugString();
        }
    }
}
