// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

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

        // Bit-mixing function to distribute sequential UniqueStyleString.id values across all 32 bits
        // Required because IDs are sequential (0, 1, 2, 3...), and without mixing, Hash2 (upper 14 bits)
        // would be 0 for the first ~1000 IDs, causing massive collisions on Bloom filter slot 0
        public static int MixBits(int id)
        {
            // Use golden ratio prime for good bit avalanche
            unchecked
            {
                uint x = (uint)id * 2654435761u;
                x ^= x >> 16;
                return (int)x;
            }
        }
    }

    [Serializable]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal class StyleComplexSelector
    {
        // Hash keys for the most relevant parts of a complex selector to use against the style sheet's Bloom filter.
        [NonSerialized] public Hashes ancestorHashes;

        [SerializeField]
        Specificity m_Specificity;

        // This "score" is calculated according to the enclosing complex selector specificity
        public Specificity specificity
        {
            get => m_Specificity;
            internal set => m_Specificity = value;
        }

        // This reference is set at runtime as convenience, but is not serialized
        [field:NonSerialized]
        public StyleRule rule
        {
            get;
            [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
            internal set;
        }

        public bool isSimple => selectors?.Length == 1;

        [SerializeField]
        StyleSelector[] m_Selectors = Array.Empty<StyleSelector>();

        public StyleSelector[] selectors
        {
            get => m_Selectors;
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            internal set => m_Selectors = value;
        }

        internal StyleComplexSelector()
        {
        }

        public bool TrySetSelectorsFromString(string complexSelectorStr, out string error)
        {
            if (!SelectorUtility.ExtractSelectorsAndSpecificityFromString(complexSelectorStr, out var newSelectors, out var newSpecificity, out error))
                return false;
            selectors = newSelectors;
            specificity = newSpecificity;
            error = null;
            return true;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        [SerializeField]
        internal int ruleIndex;

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

        internal void CachePseudoStateMasks(StyleSheet styleSheet)
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
                bool allValid = true;
                for (int i = 0; i < selector.parts.Length && allValid; i++)
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
                            if(styleSheet != null)
                                Debug.LogWarningFormat(styleSheet, "Unknown pseudo class \"{0}\" in StyleSheet {1}", parts[i].value, styleSheet.name);
                            allValid = false;
                        }
                    }
                }

                if (allValid)
                {
                    selector.pseudoStateMask = (int)pseudoClassMask;
                    selector.negatedPseudoStateMask = (int)negatedPseudoClassMask;
                }
                else
                {
                    selector.pseudoStateMask = StyleSelector.InvalidPseudoStateMask;
                    selector.negatedPseudoStateMask = StyleSelector.InvalidPseudoStateMask;
                }

            }
        }

        public override string ToString()
        {
            return string.Format("[{0}]", string.Join<StyleSelector>(", ", m_Selectors));
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

        static readonly List<StyleSelectorPart> s_HashList = new ();

        internal unsafe void CalculateHashes()
        {
            // First, cache UniqueStyleString IDs on all ID/Class/Type parts
            // This ensures all selector names are registered in the UniqueStyleString system
            for (int i = 0; i < selectors.Length; i++)
            {
                var parts = selectors[i].parts;
                for (int j = 0; j < parts.Length; j++)
                {
                    if (parts[j].type == StyleSelectorType.ID ||
                        parts[j].type == StyleSelectorType.Class ||
                        parts[j].type == StyleSelectorType.Type)
                    {
                        var uss = new UniqueStyleString(parts[j].value);
                        parts[j].cachedUniqueStyleStringId = uss.id;
                    }
                    else
                    {
                        // Initialize to -1 for non-cacheable selector types
                        parts[j].cachedUniqueStyleStringId = -1;
                    }
                }
            }

            if (isSimple)
                return;

            // Collect all selector parts except for the last selector, as a visual element was already
            // matched against the last selector when the time comes to query the Bloom filter.
            for (int i = selectors.Length - 2; i > -1; i--)
            {
                s_HashList.AddRange(selectors[i].parts);
            }

            s_HashList.RemoveAll(p =>
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
            s_HashList.Sort(StyleSelectorPartCompare);

            // Add unique parts from left to right.
            bool isFirstEntry = true;

            StyleSelectorType lastType = StyleSelectorType.Unknown;
            string lastValue = "";

            int partIndex = 0;

            int max = Math.Min(Hashes.kSize, s_HashList.Count);
            for (int i = 0; i < max; i++)
            {
                if (isFirstEntry)
                {
                    isFirstEntry = false;
                }
                else
                {
                    // Skip duplicate parts
                    while ((partIndex < s_HashList.Count) && s_HashList[partIndex].type == lastType && s_HashList[partIndex].value == lastValue)
                    {
                        partIndex++;
                    }

                    if (partIndex == s_HashList.Count)
                        break;
                }

                lastType = s_HashList[partIndex].type;
                lastValue = s_HashList[partIndex].value;

                // Get the cached UniqueStyleString id (set in the upfront loop)
                // Use bit mixing to distribute sequential IDs across all 32 bits
                int uniqueId = s_HashList[partIndex].cachedUniqueStyleStringId;

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
                ancestorHashes.hashes[i] = Hashes.MixBits(uniqueId) * (int)salt;
            }

            s_HashList.Clear();
        }
    }
}
