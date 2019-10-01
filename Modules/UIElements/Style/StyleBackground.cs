// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    public struct StyleBackground : IStyleValue<Background>, IEquatable<StyleBackground>
    {
        public Background value
        {
            get { return m_Keyword == StyleKeyword.Undefined ? m_Value : new Background(); }
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

        public StyleBackground(Background v)
            : this(v, StyleKeyword.Undefined)
        {}

        public StyleBackground(Texture2D v)
            : this(v, StyleKeyword.Undefined)
        {}

        public StyleBackground(VectorImage v)
            : this(v, StyleKeyword.Undefined)
        {}

        public StyleBackground(StyleKeyword keyword)
            : this(new Background(), keyword)
        {}

        internal StyleBackground(Texture2D v, StyleKeyword keyword)
            : this(Background.FromTexture2D(v), keyword)
        {}

        internal StyleBackground(VectorImage v, StyleKeyword keyword)
            : this(Background.FromVectorImage(v), keyword)
        {}

        internal StyleBackground(GCHandle gcHandle, StyleKeyword keyword)
            : this(gcHandle.IsAllocated ? Background.FromObject(gcHandle.Target) : new Background(), keyword)
        {}

        internal StyleBackground(Background v, StyleKeyword keyword)
        {
            m_Keyword = keyword;
            m_Value = v;
        }

        private StyleKeyword m_Keyword;
        private Background m_Value;

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
            if (!(obj is StyleBackground))
            {
                return false;
            }

            var v = (StyleBackground)obj;
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
