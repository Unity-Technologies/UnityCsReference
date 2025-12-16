// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Style value that can be either a <see cref="Material"/> or a <see cref="StyleKeyword"/>.
    /// </summary>
    [Serializable]
    public struct StyleMaterialDefinition : IStyleValue<MaterialDefinition>, IEquatable<StyleMaterialDefinition>
    {
        /// <summary>
        /// The <see cref="Material"/> value.
        /// </summary>
        public MaterialDefinition value
        {
            get { return m_Keyword == StyleKeyword.Undefined ? m_Value : default; }
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
        /// Creates from either a <see cref="MaterialDefinition"/>.
        /// </summary>
        public StyleMaterialDefinition(MaterialDefinition m)
            : this(m, StyleKeyword.Undefined)
        { }

        /// <summary>
        /// Creates from either a <see cref="Material"/> or a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleMaterialDefinition(Material m)
            : this(m, StyleKeyword.Undefined)
        {}

        public StyleMaterialDefinition(StyleKeyword keyword)
            : this(null, keyword)
        { }

        internal StyleMaterialDefinition(MaterialDefinition m, StyleKeyword keyword)
        {
            m_Keyword = keyword;
            m_Value = m;
        }

        [SerializeField]
        private MaterialDefinition m_Value;
        [SerializeField]
        private StyleKeyword m_Keyword;

        /// <undoc/>
        public static bool operator==(StyleMaterialDefinition lhs, StyleMaterialDefinition rhs)
        {
            return lhs.m_Keyword == rhs.m_Keyword && lhs.m_Value == rhs.m_Value;
        }

        /// <undoc/>
        public static bool operator!=(StyleMaterialDefinition lhs, StyleMaterialDefinition rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public static implicit operator StyleMaterialDefinition(StyleKeyword keyword)
        {
            return new StyleMaterialDefinition(keyword);
        }
        /// <undoc/>
        public static implicit operator StyleMaterialDefinition(MaterialDefinition m)
        {
            return new StyleMaterialDefinition(m);
        }
        /// <undoc/>
        public static implicit operator StyleMaterialDefinition(Material m)
        {
            return new StyleMaterialDefinition(m);
        }

        /// <undoc/>
        /// <undoc/>
        public bool Equals(StyleMaterialDefinition other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is StyleMaterialDefinition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((m_Value != null ? m_Value.GetHashCode() : 0) * 397) ^ (int)m_Keyword;
            }
        }

        public override string ToString()
        {
            return this.DebugString();
        }
    }
}
