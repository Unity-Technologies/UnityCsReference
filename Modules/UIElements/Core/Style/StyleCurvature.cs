// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Represents a style value that can be either a <see cref="Curvature"/> or a <see cref="StyleKeyword"/>.
    /// </summary>
    [Serializable]
    public struct StyleCurvature : IStyleValue<Curvature>, IEquatable<StyleCurvature>
    {
        /// <summary>
        /// The <see cref="Curvature"/> value.
        /// </summary>
        public Curvature value
        {
            get
            {
                return m_Keyword switch
                {
                    StyleKeyword.Undefined => m_Value,
                    StyleKeyword.Null => Curvature.None(),
                    StyleKeyword.None => Curvature.None(),
                    StyleKeyword.Initial => Curvature.Initial(),
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
        /// Creates a StyleCurvature from a <see cref="Curvature"/>.
        /// </summary>
        public StyleCurvature(Curvature v)
            : this(v, StyleKeyword.Undefined)
        {}

        /// <summary>
        /// Creates a StyleCurvature from a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleCurvature(StyleKeyword keyword)
            : this(default(Curvature), keyword)
        {}

        internal StyleCurvature(Curvature v, StyleKeyword keyword)
        {
            m_Keyword = keyword;
            m_Value = v;
        }

        [SerializeField]
        private Curvature m_Value;
        [SerializeField]
        private StyleKeyword m_Keyword;

        /// <undoc/>
        public static bool operator==(StyleCurvature lhs, StyleCurvature rhs)
        {
            return lhs.m_Keyword == rhs.m_Keyword && lhs.m_Value == rhs.m_Value;
        }

        /// <undoc/>
        public static bool operator!=(StyleCurvature lhs, StyleCurvature rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public static implicit operator StyleCurvature(StyleKeyword keyword)
        {
            return new StyleCurvature(keyword);
        }

        /// <undoc/>
        public static implicit operator StyleCurvature(Curvature v)
        {
            return new StyleCurvature(v);
        }

        /// <undoc/>
        public bool Equals(StyleCurvature other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is StyleCurvature other && Equals(other);
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
