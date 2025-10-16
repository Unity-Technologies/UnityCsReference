// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal static class SelectorUtility
    {
        private const string k_DescendantSymbol = ">";

        public static bool ExtractSelectorsAndSpecificityFromString(
            string complexSelectorStr,
            out StyleSelector[] selectors,
            out int specificity,
            out string error)
        {
            selectors = null;
            specificity = -1;

            var selectorSplit = complexSelectorStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var fullSpecificity = CSSSpec.GetSelectorSpecificity(complexSelectorStr);
            if (fullSpecificity == CSSSpec.InvalidSpecificityScore)
            {
                error = $"Selector '{complexSelectorStr}' is invalid: failed to calculate selector specificity.";
                return false;
            }

            var simpleSelectors = new List<StyleSelector>();
            var previousRelationship = StyleSelectorRelationship.None;
            foreach (var simpleSelectorStr in selectorSplit)
            {
                if (simpleSelectorStr == k_DescendantSymbol)
                {
                    previousRelationship = StyleSelectorRelationship.Child;
                    continue;
                }

                if (!CSSSpec.ParseSelector(simpleSelectorStr, out var parts))
                {
                    error = $"Selector '{complexSelectorStr}' is invalid: the selector could not be parsed.";
                    return false;
                }

                for (var i = 0; i < parts.Length; ++i)
                {
                    var part = parts[i];
                    switch (part.type)
                    {
                        case StyleSelectorType.Unknown:
                            error = $"Selector '{complexSelectorStr}' is invalid: the selector contains unknown parts.";
                            return false;
                        case StyleSelectorType.RecursivePseudoClass:
                            error = $"Selector '{complexSelectorStr}' is invalid: the selector contains recursive parts.";
                            return false;
                        default:
                            break;
                    }
                }

                var simpleSelector = new StyleSelector
                {
                    parts = parts,
                    previousRelationship = previousRelationship
                };
                simpleSelectors.Add(simpleSelector);

                // This is the default (if no > came before).
                previousRelationship = StyleSelectorRelationship.Descendent;
            }

            selectors = simpleSelectors.ToArray();
            specificity = fullSpecificity;
            error = null;
            return true;
        }

        // Used in tests
        public static bool CompareSelectors(StyleComplexSelector lhs, StyleComplexSelector rhs)
        {
            if (lhs.isSimple != rhs.isSimple
                || lhs.specificity != rhs.specificity
                || lhs.selectors.Length != rhs.selectors.Length)
                return false;

            for(var i = 0; i < lhs.selectors.Length; ++i)
            {
                var lSelector = lhs.selectors[i];
                var rSelector = rhs.selectors[i];

                if (lSelector.parts.Length != rSelector.parts.Length)
                    return false;

                if (lSelector.previousRelationship != rSelector.previousRelationship)
                    return false;

                for (var j = 0; j < lSelector.parts.Length; ++j)
                {
                    if (!EqualityComparer<StyleSelectorPart>.Default.Equals(lSelector.parts[j], rSelector.parts[j]))
                        return false;
                }
            }

            return true;
        }
    }
}
