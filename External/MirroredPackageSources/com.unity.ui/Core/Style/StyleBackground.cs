using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Style value that can be either a <see cref="Background"/> or a <see cref="StyleKeyword"/>.
    /// </summary>
    public struct StyleBackground : IStyleValue<Background>, IEquatable<StyleBackground>
    {
        /// <summary>
        /// The <see cref="Background"/> value.
        /// </summary>
        public Background value
        {
            get { return m_Keyword == StyleKeyword.Undefined ? m_Value : new Background(); }
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
        /// Creates from either a <see cref="Background"/> or a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleBackground(Background v)
            : this(v, StyleKeyword.Undefined)
        {}

        /// <summary>
        /// Creates from either a <see cref="Background"/> or a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleBackground(Texture2D v)
            : this(v, StyleKeyword.Undefined)
        {}

        /// <summary>
        /// Creates from either a <see cref="Background"/> or a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleBackground(Sprite v)
            : this(v, StyleKeyword.Undefined)
        {}

        /// <summary>
        /// Creates from either a <see cref="Background"/> or a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleBackground(VectorImage v)
            : this(v, StyleKeyword.Undefined)
        {}

        /// <summary>
        /// Creates from either a <see cref="Background"/> or a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleBackground(StyleKeyword keyword)
            : this(new Background(), keyword)
        {}

        internal StyleBackground(Texture2D v, StyleKeyword keyword)
            : this(Background.FromTexture2D(v), keyword)
        {}

        internal StyleBackground(Sprite v, StyleKeyword keyword)
            : this(Background.FromSprite(v), keyword)
        {}

        internal StyleBackground(VectorImage v, StyleKeyword keyword)
            : this(Background.FromVectorImage(v), keyword)
        {}

        internal StyleBackground(Background v, StyleKeyword keyword)
        {
            m_Keyword = keyword;
            m_Value = v;
        }

        private Background m_Value;
        private StyleKeyword m_Keyword;

        public static bool operator==(StyleBackground lhs, StyleBackground rhs)
        {
            return lhs.m_Keyword == rhs.m_Keyword && lhs.m_Value == rhs.m_Value;
        }

        public static bool operator!=(StyleBackground lhs, StyleBackground rhs)
        {
            return !(lhs == rhs);
        }

        public static implicit operator StyleBackground(StyleKeyword keyword)
        {
            return new StyleBackground(keyword);
        }

        public static implicit operator StyleBackground(Background v)
        {
            return new StyleBackground(v);
        }

        public static implicit operator StyleBackground(Texture2D v)
        {
            return new StyleBackground(v);
        }

        public bool Equals(StyleBackground other)
        {
            return other == this;
        }

        public override bool Equals(object obj)
        {
            return obj is StyleBackground other && Equals(other);
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
