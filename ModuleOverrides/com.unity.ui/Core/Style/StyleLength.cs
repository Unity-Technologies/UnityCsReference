// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Style value that can be either a <see cref="Length"/> or a <see cref="StyleKeyword"/>.
    /// </summary>
    public struct StyleLength : IStyleValue<Length>, IEquatable<StyleLength>
    {
        /// <summary>
        /// The <see cref="Length"/> value.
        /// </summary>
        public Length value
        {
            get
            {
                if (m_Keyword == StyleKeyword.Auto ||
                    m_Keyword == StyleKeyword.None ||
                    m_Keyword == StyleKeyword.Undefined)
                    return m_Value;
                else
                    return default(Length);
            }
            set
            {
                if (value.IsAuto())
                    m_Keyword = StyleKeyword.Auto;
                else if (value.IsNone())
                    m_Keyword = StyleKeyword.None;
                else
                    m_Keyword = StyleKeyword.Undefined;

                m_Value = value;
            }
        }

        /// <summary>
        /// The style keyword.
        /// </summary>
        public StyleKeyword keyword
        {
            get { return m_Keyword; }
            set
            {
                m_Keyword = value;

                if (m_Keyword == StyleKeyword.Auto)
                {
                    m_Value = Length.Auto();
                }
                else if (m_Keyword == StyleKeyword.None)
                {
                    m_Value = Length.None();
                }
                else if (m_Keyword != StyleKeyword.Undefined)
                {
                    m_Value = default(Length);
                }
            }
        }

        /// <summary>
        /// Creates from either a <see cref="Length"/> or a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleLength(float v)
            : this(new Length(v, LengthUnit.Pixel), StyleKeyword.Undefined)
        {}

        /// <summary>
        /// Creates from either a <see cref="Length"/> or a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleLength(Length v)
            : this(v, StyleKeyword.Undefined)
        {}

        /// <summary>
        /// Creates from either a <see cref="Length"/> or a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleLength(StyleKeyword keyword)
            : this(default(Length), keyword)
        {}

        internal StyleLength(Length v, StyleKeyword keyword)
        {
            m_Keyword = keyword;
            m_Value = v;

            if (v.IsAuto())
                m_Keyword = StyleKeyword.Auto;
            else if (v.IsNone())
                m_Keyword = StyleKeyword.None;
        }

        private Length m_Value;
        private StyleKeyword m_Keyword;

        /// <undoc/>
        public static bool operator==(StyleLength lhs, StyleLength rhs)
        {
            return lhs.m_Keyword == rhs.m_Keyword && lhs.m_Value == rhs.m_Value;
        }

        /// <undoc/>
        public static bool operator!=(StyleLength lhs, StyleLength rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public static implicit operator StyleLength(StyleKeyword keyword)
        {
            return new StyleLength(keyword);
        }

        /// <undoc/>
        public static implicit operator StyleLength(float v)
        {
            return new StyleLength(v);
        }

        /// <undoc/>
        public static implicit operator StyleLength(Length v)
        {
            return new StyleLength(v);
        }

        /// <undoc/>
        public bool Equals(StyleLength other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is StyleLength other && Equals(other);
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
