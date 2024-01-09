// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using System.Linq;

namespace UnityEngine.UIElements
{
    // Salt values used to create distinct selector part hash results for the Bloom filter selector search optimization.
    enum Salt
    {
        TagNameSalt = 13,
        IdSalt = 17,
        ClassSalt = 19
    }

    // Storage for up to 4 selector part hash values; used to query the Bloom filter for a visual element during a visual tree style update hierarchical traversal.
    unsafe struct Hashes
    {
        public const int kSize = 4;
        public fixed int hashes[kSize];
    }

    [Serializable]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class StyleComplexSelector : ISerializationCallbackReceiver
    {
        // Hash keys for the most relevant parts of a complex selector to use against the style sheet's Bloom filter.
        [NonSerialized] public Hashes ancestorHashes;

        [SerializeField]
        int m_Specificity;

        // This "score" is calculated according to the enclosing complex selector specificity
        public int specificity
        {
            get
            {
                return m_Specificity;
            }
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            internal set
            {
                m_Specificity = value;
            }
        }

        // This reference is set at runtime as convenience, but is not serialized
        public StyleRule rule
        {
            get;
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            internal set;
        }

        // A complex selector can be considered simple if it's made of only one selector
        [NonSerialized]
        private bool m_isSimple;

        public bool isSimple
        {
            get
            {
                return m_isSimple;
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
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            internal set
            {
                m_Selectors = value;
                m_isSimple = m_Selectors.Length == 1;
            }
        }

        public void OnBeforeSerialize() {}

        public virtual void OnAfterDeserialize()
        {
            m_isSimple = m_Selectors.Length == 1;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        [SerializeField]
        internal int ruleIndex;

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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

        // Sort StyleSelectorPart elements in decreasing type order, then decreasing value order.
        private static int StyleSelectorPartCompare(StyleSelectorPart x, StyleSelectorPart y)
        {
            if (y.type < x.type)
                return -1;
            else if (y.type > x.type)
                return 1;
            else
                return y.value.CompareTo(x.value);
        }

        static List<StyleSelectorPart> m_HashList = new List<StyleSelectorPart>();

        unsafe internal void CalculateHashes()
        {
            if (isSimple)
                return;

            // Collect all selector parts except for the last selector, as a visual element was already
            // matched against the last selector when the time comes to query the Bloom filter.
            for (int i = selectors.Length - 2; i > -1; i--)
            {
                m_HashList.AddRange(selectors[i].parts);
            }

            m_HashList.RemoveAll(p =>
                p.type != StyleSelectorType.Class
                && p.type != StyleSelectorType.ID
                && p.type != StyleSelectorType.Type);

            // The ancestorHashes member contains up to 4 hash keys that will be used to query the Bloom
            // filter during a hierarchical Visual Element traversal. The Bloom filter contains the hash
            // values of all the ID, Class and Type strings of visited Visual Elements. In order for any
            // complex selector to match, all of its hash keys must be found in the Bloom filter. We use
            // the most relevant parts for the search for performance reasons and it can't produce false
            // rejections.

            // Sort parts in decreasing type order, then value order, i.e. in ID, Class, Type order.
            m_HashList.Sort(StyleSelectorPartCompare);

            // Add unique parts from left to right.
            bool isFirstEntry = true;

            StyleSelectorType lastType = StyleSelectorType.Unknown;
            string lastValue = "";

            int partIndex = 0;

            int max = Math.Min(Hashes.kSize, m_HashList.Count);
            for (int i = 0; i < max; i++)
            {
                if (isFirstEntry)
                {
                    isFirstEntry = false;
                }
                else
                {
                    // Skip duplicate parts
                    while ((partIndex < m_HashList.Count) && m_HashList[partIndex].type == lastType && m_HashList[partIndex].value == lastValue)
                    {
                        partIndex++;
                    }

                    if (partIndex == m_HashList.Count)
                        break;
                }

                lastType = m_HashList[partIndex].type;
                lastValue = m_HashList[partIndex].value;

                Salt salt;
                if (lastType == StyleSelectorType.ID)
                {
                    salt = Salt.IdSalt;
                }
                else if (lastType == StyleSelectorType.Class)
                {
                    salt = Salt.ClassSalt;
                }
                else
                {
                    salt = Salt.TagNameSalt;
                }
                ancestorHashes.hashes[i] = lastValue.GetHashCode() * (int)salt;
            }

            m_HashList.Clear();
        }
    }
}
