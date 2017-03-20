// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;

namespace UnityEngine.StyleSheets
{
    [Serializable]
    internal class StyleSelector
    {
        [SerializeField]
        StyleSelectorPart[] m_Parts;

        public StyleSelectorPart[] parts
        {
            get
            {
                return m_Parts;
            }
            internal set
            {
                m_Parts = value;
            }
        }

        [SerializeField]
        StyleSelectorRelationship m_PreviousRelationship;

        public StyleSelectorRelationship previousRelationship
        {
            get
            {
                return m_PreviousRelationship;
            }
            internal set
            {
                m_PreviousRelationship = value;
            }
        }

        public override string ToString()
        {
            return string.Join(", ", parts.Select(p => p.ToString()).ToArray());
        }
    }
}
