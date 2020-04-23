using System;
using System.Linq;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
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

        // those two variables are initialized lazily by UIElements when it first sees the selector
        // it's convenient to store them here to avoid using an associative container
        internal int pseudoStateMask = -1;
        internal int negatedPseudoStateMask = -1;

        public override string ToString()
        {
            return string.Join(", ", parts.Select(p => p.ToString()).ToArray());
        }
    }
}
