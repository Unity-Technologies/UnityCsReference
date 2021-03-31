using System;
using System.Runtime.InteropServices;
using UnityEngine.TextCore.Text;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Style value that can be either a <see cref="FontDefinition"/> or a <see cref="StyleKeyword"/>.
    /// </summary>
    public struct StyleFontDefinition : IStyleValue<FontDefinition>, IEquatable<StyleFontDefinition>
    {
        /// <summary>
        /// The actual value of the definition.
        /// </summary>
        /// <remarks>
        /// This value is only valid if the <see cref="keyword"/> is <see cref="StyleKeyword.Undefined"/>.
        /// </remarks>
        public FontDefinition value
        {
            get { return m_Keyword == StyleKeyword.Undefined ? m_Value : new FontDefinition(); }
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
        /// Creates from either a <see cref="FontDefinition"/> or a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleFontDefinition(FontDefinition f)
            : this(f, StyleKeyword.Undefined)
        {}

        /// <summary>
        /// Creates from either a <see cref="FontDefinition"/> or a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleFontDefinition(FontAsset f)
            : this(f, StyleKeyword.Undefined)
        {}

        public StyleFontDefinition(Font f)
            : this(f, StyleKeyword.Undefined)
        {}

        /// <summary>
        /// Creates from either a <see cref="FontDefinition"/> or a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleFontDefinition(StyleKeyword keyword)
            : this(new FontDefinition(), keyword)
        {}

        internal StyleFontDefinition(object obj, StyleKeyword keyword)
            : this(FontDefinition.FromObject(obj), keyword)
        {
        }

        internal StyleFontDefinition(object obj)
            : this(FontDefinition.FromObject(obj), StyleKeyword.Undefined)
        {
        }

        internal StyleFontDefinition(FontAsset f, StyleKeyword keyword)
            : this(FontDefinition.FromSDFFont(f), keyword)
        {}


        internal StyleFontDefinition(Font f, StyleKeyword keyword)
            : this(FontDefinition.FromFont(f), keyword)
        {}


        internal StyleFontDefinition(GCHandle gcHandle, StyleKeyword keyword)
            : this(gcHandle.IsAllocated ? FontDefinition.FromObject(gcHandle.Target) : new FontDefinition(), keyword)
        {}

        internal StyleFontDefinition(FontDefinition f, StyleKeyword keyword)
        {
            m_Keyword = keyword;
            m_Value = f;
        }

        internal StyleFontDefinition(StyleFontDefinition sfd)
        {
            m_Keyword = sfd.keyword;
            m_Value = sfd.value;
        }

        private StyleKeyword m_Keyword;
        private FontDefinition m_Value;

        /// <undoc/>
        public static implicit operator StyleFontDefinition(StyleKeyword keyword)
        {
            return new StyleFontDefinition(keyword);
        }

        /// <undoc/>
        public static implicit operator StyleFontDefinition(FontDefinition f)
        {
            return new StyleFontDefinition(f);
        }

        /// <undoc/>
        public bool Equals(StyleFontDefinition other)
        {
            return m_Keyword == other.m_Keyword && m_Value.Equals(other.m_Value);
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is StyleFontDefinition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)m_Keyword * 397) ^ m_Value.GetHashCode();
            }
        }

        /// <undoc/>
        public static bool operator==(StyleFontDefinition left, StyleFontDefinition right)
        {
            return left.Equals(right);
        }

        /// <undoc/>
        public static bool operator!=(StyleFontDefinition left, StyleFontDefinition right)
        {
            return !left.Equals(right);
        }
    }
}
