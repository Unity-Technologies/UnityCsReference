// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Represents a style value that can be either a <see cref="TransformOrigin"/> or a <see cref="StyleKeyword"/>.
    /// </summary>
    public struct StyleTransformOrigin : IStyleValue<TransformOrigin>, IEquatable<StyleTransformOrigin>
    {
        /// <summary>
        /// The <see cref="TransformOrigin"/> value.
        /// </summary>
        public TransformOrigin value
        {
            get
            {
                // SD: Changed to provide an interpretation of Initial and Null for the debugger, that takes the StyleTransformOrigin and need to display some value in the fields.
                // This is probably subject to change in the future.
                return m_Keyword switch
                {
                    StyleKeyword.Undefined => m_Value,
                    StyleKeyword.Null => TransformOrigin.Initial(),
                    StyleKeyword.None => TransformOrigin.Initial(),
                    StyleKeyword.Initial => TransformOrigin.Initial(),
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
        /// Creates a new StyleTransformOrigin from either a <see cref="TransformOrigin"/> or a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleTransformOrigin(TransformOrigin v)
            : this(v, StyleKeyword.Undefined)
        {}

        /// <summary>
        /// Creates  a new StyleTransformOrigin from either a <see cref="TransformOrigin"/> or a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleTransformOrigin(StyleKeyword keyword)
            : this(default(TransformOrigin), keyword)
        {}

        internal StyleTransformOrigin(TransformOrigin v, StyleKeyword keyword)
        {
            m_Keyword = keyword;
            m_Value = v;
        }

        private TransformOrigin m_Value;
        private StyleKeyword m_Keyword;

        /// <undoc/>
        public static bool operator==(StyleTransformOrigin lhs, StyleTransformOrigin rhs)
        {
            return lhs.m_Keyword == rhs.m_Keyword && lhs.m_Value == rhs.m_Value;
        }

        /// <undoc/>
        public static bool operator!=(StyleTransformOrigin lhs, StyleTransformOrigin rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public static implicit operator StyleTransformOrigin(StyleKeyword keyword)
        {
            return new StyleTransformOrigin(keyword);
        }

        /// <undoc/>
        public static implicit operator StyleTransformOrigin(TransformOrigin v)
        {
            return new StyleTransformOrigin(v);
        }

        /// <undoc/>
        public bool Equals(StyleTransformOrigin other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is StyleTransformOrigin other && Equals(other);
        }

        /// <undoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return (m_Value.GetHashCode() * 397) ^ (int)m_Keyword;
            }
        }

        /// <undoc/>
        public override string ToString()
        {
            return this.DebugString();
        }
    }
}
