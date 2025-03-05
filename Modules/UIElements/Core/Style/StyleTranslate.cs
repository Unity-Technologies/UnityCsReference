// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Style value that can be either a <see cref="Translate"/> or a <see cref="StyleKeyword"/>.
    /// </summary>
    public struct StyleTranslate : IStyleValue<Translate>, IEquatable<StyleTranslate>
    {
        /// <summary>
        /// The <see cref="Translate"/> value.
        /// </summary>
        public Translate value
        {
            get
            {
                // SD: Changed to provide an interpretation of Initial and Null for the debugger, that takes the StyleTranslate and need to display some value in the fields.
                // This is probably subject to change in the future.
                return m_Keyword switch
                {
                    StyleKeyword.Undefined => m_Value,
                    StyleKeyword.Null => Translate.None(),
                    StyleKeyword.None => Translate.None(),
                    StyleKeyword.Initial => Translate.None(),
                    _ => throw new NotImplementedException(),
                };
            }
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
        /// Creates from either a <see cref="Translate"/> or a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleTranslate(Translate v)
            : this(v, StyleKeyword.Undefined)
        {}

        /// <summary>
        /// Creates from either a <see cref="Translate"/> or a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleTranslate(StyleKeyword keyword)
            : this(default(Translate), keyword)
        {}

        internal StyleTranslate(Translate v, StyleKeyword keyword)
        {
            m_Keyword = keyword;
            m_Value = v;
        }

        private Translate m_Value;
        private StyleKeyword m_Keyword;

        /// <undoc/>
        public static bool operator==(StyleTranslate lhs, StyleTranslate rhs)
        {
            return lhs.m_Keyword == rhs.m_Keyword && lhs.m_Value == rhs.m_Value;
        }

        /// <undoc/>
        public static bool operator!=(StyleTranslate lhs, StyleTranslate rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public static implicit operator StyleTranslate(StyleKeyword keyword)
        {
            return new StyleTranslate(keyword);
        }

        /// <undoc/>
        public static implicit operator StyleTranslate(Translate v)
        {
            return new StyleTranslate(v);
        }

        /// <undoc/>
        public static implicit operator StyleTranslate(Vector3 v)
        {
            return new StyleTranslate(v);
        }

        /// <undoc/>
        public static implicit operator StyleTranslate(Vector2 v)
        {
            return new StyleTranslate(v);
        }

        /// <undoc/>
        public bool Equals(StyleTranslate other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is StyleTranslate other && Equals(other);
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
