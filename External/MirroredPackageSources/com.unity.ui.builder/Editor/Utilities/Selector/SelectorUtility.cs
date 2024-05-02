using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    static class SelectorUtility
    {
        public static bool TryCreateSelector(string complexSelectorStr, out StyleComplexSelector selector, out string error)
        {
            // Remove extra whitespace.
            var selectorSplit = complexSelectorStr.Split(' ');
            complexSelectorStr = string.Join(" ", selectorSplit);

            // Create rule.
            var rule = new StyleRule
            {
                line = -1,
                properties = Array.Empty<StyleProperty>()
            };

            // Create selector.
            selector = new StyleComplexSelector
            {
                rule = rule
            };
            var initResult = StyleComplexSelectorExtensions.InitializeSelector(selector, complexSelectorStr, out error);
            return initResult;
        }

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
