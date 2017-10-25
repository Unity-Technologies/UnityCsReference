// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine.Bindings;

namespace UnityEngine.StyleSheets
{
    [Serializable]
    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
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
            [VisibleToOtherModules("UnityEngine.UIElementsModule")]
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
            [VisibleToOtherModules("UnityEngine.UIElementsModule")]
            internal set
            {
                m_PreviousRelationship = value;
            }
        }

        // those two variables are initialized lazily by UIElements when it first sees the selector
        // it's convenient to store them here to avoid using an associative container
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal int pseudoStateMask = -1;
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal int negatedPseudoStateMask = -1;

        public override string ToString()
        {
            return string.Join(", ", parts.Select(p => p.ToString()).ToArray());
        }
    }
}
