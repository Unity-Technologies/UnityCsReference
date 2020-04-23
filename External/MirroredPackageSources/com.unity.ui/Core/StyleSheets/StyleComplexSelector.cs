using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using System.Linq;

namespace UnityEngine.UIElements
{
    [Serializable]
    internal class StyleComplexSelector
    {
        [SerializeField]
        int m_Specificity;

        // This "score" is calculated according to the enclosing complex selector specificity
        public int specificity
        {
            get
            {
                return m_Specificity;
            }
            internal set
            {
                m_Specificity = value;
            }
        }

        // This reference is set at runtime as convenience, but is not serialzed
        public StyleRule rule { get; internal set; }

        // A complex selector can be considered simple if it's made of only one selector
        public bool isSimple
        {
            get
            {
                return selectors.Length == 1;
            }
        }

        [SerializeField]
        StyleSelector[] m_Selectors;

        public StyleSelector[] selectors
        {
            get
            {
                return m_Selectors;
            }
            internal set
            {
                m_Selectors = value;
            }
        }

        [SerializeField]
        internal int ruleIndex;

        [NonSerialized]
        // Points to the possible next selector that indexes to the same lookup table in StyleSheet
        internal StyleComplexSelector nextInTable;

        [NonSerialized]
        internal int orderInStyleSheet;

        struct PseudoStateData
        {
            public readonly PseudoStates state;
            public readonly bool negate;

            public PseudoStateData(PseudoStates state, bool negate)
            {
                this.state = state;
                this.negate = negate;
            }
        }
        static Dictionary<string, PseudoStateData> s_PseudoStates;

        internal void CachePseudoStateMasks()
        {
            // lazily build a cache of pseudo state names
            if (s_PseudoStates == null)
            {
                s_PseudoStates = new Dictionary<string, PseudoStateData>();
                s_PseudoStates["active"] = new PseudoStateData(PseudoStates.Active, false);
                s_PseudoStates["hover"] = new PseudoStateData(PseudoStates.Hover, false);
                s_PseudoStates["checked"] = new PseudoStateData(PseudoStates.Checked, false);
                s_PseudoStates["selected"] = new PseudoStateData(PseudoStates.Checked, false); //for backward-compatibility
                s_PseudoStates["disabled"] = new PseudoStateData(PseudoStates.Disabled, false);
                s_PseudoStates["focus"] = new PseudoStateData(PseudoStates.Focus, false);
                s_PseudoStates["root"] = new PseudoStateData(PseudoStates.Root, false);

                // A few substates can be negated, meaning them match if the flag is not set
                s_PseudoStates["inactive"] = new PseudoStateData(PseudoStates.Active, true);
                s_PseudoStates["enabled"] = new PseudoStateData(PseudoStates.Disabled, true);
            }

            for (int j = 0, subCount = selectors.Length; j < subCount; j++)
            {
                StyleSelector selector = selectors[j];
                StyleSelectorPart[] parts = selector.parts;
                PseudoStates pseudoClassMask = 0;
                PseudoStates negatedPseudoClassMask = 0;
                for (int i = 0; i < selector.parts.Length; i++)
                {
                    if (selector.parts[i].type == StyleSelectorType.PseudoClass)
                    {
                        PseudoStateData data;
                        if (s_PseudoStates.TryGetValue(parts[i].value, out data))
                        {
                            if (!data.negate)
                                pseudoClassMask |= data.state;
                            else
                                negatedPseudoClassMask |= data.state;
                        }
                        else
                        {
                            Debug.LogWarningFormat("Unknown pseudo class \"{0}\"", parts[i].value);
                        }
                    }
                }
                selector.pseudoStateMask = (int)pseudoClassMask;
                selector.negatedPseudoStateMask = (int)negatedPseudoClassMask;
            }
        }

        public override string ToString()
        {
            return string.Format("[{0}]", string.Join(", ", m_Selectors.Select(x => x.ToString()).ToArray()));
        }
    }
}
