// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    public struct StyleLength : IStyleValue<Length>, IEquatable<StyleLength>
    {
        public Length value
        {
            get { return m_Keyword == StyleKeyword.Undefined ? m_Value : default(Length); }
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

        public StyleLength(float v)
            : this(new Length(v, LengthUnit.Pixel), StyleKeyword.Undefined)
        {}

        public StyleLength(Length v)
            : this(v, StyleKeyword.Undefined)
        {}

        public StyleLength(StyleKeyword keyword)
            : this(default(Length), keyword)
        {}

        internal StyleLength(Length v, StyleKeyword keyword)
        {
            m_Keyword = keyword;
            m_Value = v;
        }

        private StyleKeyword m_Keyword;
        private Length m_Value;

        public static bool operator==(StyleLength lhs, StyleLength rhs)
        {
            return lhs.m_Keyword == rhs.m_Keyword && lhs.m_Value == rhs.m_Value;
        }

        public static bool operator!=(StyleLength lhs, StyleLength rhs)
        {
            return !(lhs == rhs);
        }

        public static implicit operator StyleLength(StyleKeyword keyword)
        {
            return new StyleLength(keyword);
        }

        public static implicit operator StyleLength(float v)
        {
            return new StyleLength(v);
        }

        public static implicit operator StyleLength(Length v)
        {
            return new StyleLength(v);
        }

        public bool Equals(StyleLength other)
        {
            return other == this;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is StyleLength))
            {
                return false;
            }

            var v = (StyleLength)obj;
            return v == this;
        }

        public override int GetHashCode()
        {
            var hashCode = -1977396678;
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
