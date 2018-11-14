// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    public struct StyleFloat : IStyleValue<float>, IEquatable<StyleFloat>
    {
        public float value
        {
            get { return m_Keyword == StyleKeyword.Undefined ? m_Value : default(float); }
            set
            {
                m_Value = value;
                m_Keyword = StyleKeyword.Undefined;
            }
        }

        internal int specificity
        {
            get { return m_Specificity; }
            set { m_Specificity = value; }
        }

        int IStyleValue<float>.specificity
        {
            get { return specificity; }
            set { specificity = value; }
        }

        public StyleKeyword keyword
        {
            get { return m_Keyword; }
            set { m_Keyword = value; }
        }

        public StyleFloat(float v)
            : this(v, StyleKeyword.Undefined)
        {}

        public StyleFloat(StyleKeyword keyword)
            : this(default(float), keyword)
        {}

        internal StyleFloat(float v, StyleKeyword keyword)
        {
            m_Specificity = StyleValueExtensions.UndefinedSpecificity;
            m_Keyword = keyword;
            m_Value = v;
        }

        internal bool Apply<U>(U other, StylePropertyApplyMode mode) where U : IStyleValue<float>
        {
            if (StyleValueExtensions.CanApply(specificity, other.specificity, mode))
            {
                value = other.value;
                keyword = other.keyword;
                specificity = other.specificity;
                return true;
            }
            return false;
        }

        bool IStyleValue<float>.Apply<U>(U other, StylePropertyApplyMode mode)
        {
            return Apply(other, mode);
        }

        private StyleKeyword m_Keyword;
        private float m_Value;
        private int m_Specificity;

        public static bool operator==(StyleFloat lhs, StyleFloat rhs)
        {
            return lhs.m_Keyword == rhs.m_Keyword && lhs.m_Value == rhs.m_Value;
        }

        public static bool operator!=(StyleFloat lhs, StyleFloat rhs)
        {
            return !(lhs == rhs);
        }

        public static implicit operator StyleFloat(StyleKeyword keyword)
        {
            return new StyleFloat(keyword);
        }

        public static implicit operator StyleFloat(float v)
        {
            return new StyleFloat(v);
        }

        public bool Equals(StyleFloat other)
        {
            return other == this;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is StyleFloat))
            {
                return false;
            }

            var v = (StyleFloat)obj;
            return v == this;
        }

        public override int GetHashCode()
        {
            var hashCode = 917506989;
            hashCode = hashCode * -1521134295 + m_Keyword.GetHashCode();
            hashCode = hashCode * -1521134295 + m_Value.GetHashCode();
            hashCode = hashCode * -1521134295 + m_Specificity.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return this.DebugString();
        }
    }
}
