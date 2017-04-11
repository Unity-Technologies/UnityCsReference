// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.StyleSheets;

namespace UnityEngine.Experimental.UIElements.StyleSheets
{
    // extension methods because StyleSelector must not depend on UIElements types
    internal static class StyleComplexSelectorExtensions
    {
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

        public static void CachePseudoStateMasks(this StyleComplexSelector complexSelector)
        {
            // If we have already cached data on this selector, skip it
            if (complexSelector.selectors[0].pseudoStateMask != -1)
                return;

            // lazily build a cache of pseudo state names
            if (s_PseudoStates == null)
            {
                s_PseudoStates = new Dictionary<string, PseudoStateData>();
                s_PseudoStates["active"] = new PseudoStateData(PseudoStates.Active, false);
                s_PseudoStates["hover"] = new PseudoStateData(PseudoStates.Hover, false);
                s_PseudoStates["checked"] = new PseudoStateData(PseudoStates.Checked, false);
                s_PseudoStates["selected"] = new PseudoStateData(PseudoStates.Selected, false);
                s_PseudoStates["disabled"] = new PseudoStateData(PseudoStates.Disabled, false);
                s_PseudoStates["focus"] = new PseudoStateData(PseudoStates.Focus, false);

                // A few substates can be negated, meaning them match if the flag is not set
                s_PseudoStates["inactive"] = new PseudoStateData(PseudoStates.Active, true);
                s_PseudoStates["enabled"] = new PseudoStateData(PseudoStates.Disabled, true);
            }

            for (int j = 0, subCount = complexSelector.selectors.Length; j < subCount; j++)
            {
                StyleSelector selector = complexSelector.selectors[j];
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
    }
}
