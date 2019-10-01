// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    public struct StyleEnum<T> : IStyleValue<T>, IEquatable<StyleEnum<T>> where T : struct, IConvertible
    {
        public T value
        {
            get { return m_Keyword == StyleKeyword.Undefined ? m_Value : default(T); }
            set
            {
                m_Value = value;
                m_Keyword = StyleKeyword.Undefined;
            }
        }

        public StyleKeyword keyword
        {
            get { return m_Keyword; }
            set { m_Keyword = value; }
        }

        public StyleEnum(T v)
            : this(v, StyleKeyword.Undefined)
        {}

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
            return lhs.m_Keyword == rhs.m_Keyword && lhs.m_Value.ToInt32(CultureInfo.InvariantCulture) == rhs.m_Value.ToInt32(CultureInfo.InvariantCulture);
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
