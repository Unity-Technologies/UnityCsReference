using System.Collections.Generic;
using System.Linq;
using UnityEditor.StyleSheets;
using System;
using UnityEngine.UIElements;
using UnityEngine;

using UnityEditor.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    internal static class StyleComplexSelectorExtensions
    {
        static readonly string s_DecendantSymbol = ">";

        internal static bool InitializeSelector(StyleComplexSelector complexSelector, string complexSelectorStr)
        {
            // Set specificity.
            int fullSpecificity = CSSSpecCopy.GetSelectorSpecificity(complexSelectorStr);
            if (fullSpecificity == 0)
            {
                Debug.LogError("Failed to calculate selector specificity: " + complexSelectorStr);
                return false;
            }
            complexSelector.specificity = fullSpecificity;

            // Remove extra whitespace.
            var selectorSplit = complexSelectorStr.Split(' ');
            complexSelectorStr = String.Join(" ", selectorSplit);

            var simpleSelectors = new List<StyleSelector>();
            var previousRelationsip = StyleSelectorRelationship.None;
            foreach (var simpleSelectorStr in selectorSplit)
            {
                if (simpleSelectorStr == s_DecendantSymbol)
                {
                    previousRelationsip = StyleSelectorRelationship.Child;
                    continue;
                }

                StyleSelectorPart[] parts;
                if (!CSSSpecCopy.ParseSelector(simpleSelectorStr, out parts))
                {
                    Debug.LogError(StyleSheetImportErrorCode.UnsupportedSelectorFormat + ": " + complexSelectorStr);
                    return false;
                }
                if (parts.Any(p => p.type == StyleSelectorType.Unknown))
                {
                    Debug.LogError(StyleSheetImportErrorCode.UnsupportedSelectorFormat + ": " + complexSelectorStr);
                    return false;
                }
                if (parts.Any(p => p.type == StyleSelectorType.RecursivePseudoClass))
                {
                    Debug.LogError(StyleSheetImportErrorCode.RecursiveSelectorDetected + ": " + complexSelectorStr);
                    return false;
                }

                var simpleSelector = new StyleSelector();
                simpleSelector.parts = parts;
                simpleSelector.previousRelationship = previousRelationsip;
                simpleSelectors.Add(simpleSelector);

                // This is the default (if no > came before).
                previousRelationsip = StyleSelectorRelationship.Descendent;
            }

            complexSelector.selectors = simpleSelectors.ToArray();

            return true;
        }

        public static void SetSelectorString(this StyleSheet styleSheet, StyleComplexSelector complexSelector, string newComplexSelectorStr)
        {
            InitializeSelector(complexSelector, newComplexSelectorStr);            
            styleSheet?.UpdateContentHash();
        }

        public static StyleProperty FindProperty(this StyleSheet styleSheet, StyleComplexSelector selector, string propertyName)
        {
            return styleSheet.FindProperty(selector.rule, propertyName);
        }

        public static StyleProperty AddProperty(
            this StyleSheet styleSheet, StyleComplexSelector selector, string name,
            string undoMessage = null)
        {
            return styleSheet.AddProperty(selector.rule, name, undoMessage);
        }

        internal static void RemoveProperty(
            this StyleSheet styleSheet, StyleComplexSelector selector,
            StyleProperty property, string undoMessage = null)
        {
            styleSheet.RemoveProperty(selector.rule, property, undoMessage);
        }

        internal static void RemoveProperty(
            this StyleSheet styleSheet, StyleComplexSelector selector, string name,
            string undoMessage = null)
        {
            styleSheet.RemoveProperty(selector.rule, name, undoMessage);
        }

        public static bool IsSelected(this StyleComplexSelector scs)
        {
            var selectionProperty = scs.FindProperty(BuilderConstants.SelectedStyleRulePropertyName);
            return selectionProperty != null;
        }
    }
}
