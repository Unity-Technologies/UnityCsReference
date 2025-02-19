// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [Serializable]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            internal set
            {
                m_PreviousRelationship = value;
            }
        }

        public const int InvalidPseudoStateMask = -1;

        // those two variables are initialized lazily by UIElements when it first sees the selector
        // it's convenient to store them here to avoid using an associative container
        internal int pseudoStateMask = InvalidPseudoStateMask;
        internal int negatedPseudoStateMask = InvalidPseudoStateMask;

        public override string ToString()
        {
            return string.Join(", ", parts.Select(p => p.ToString()).ToArray());
        }
    }
}
